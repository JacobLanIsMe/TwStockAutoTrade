using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Service
{
    public class StockTraderService : IStockTraderService
    {
        YuantaOneAPITrader objYuantaOneAPI = new YuantaOneAPITrader();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Dictionary<string, StockTrade> _stockHoldingDict = new Dictionary<string, StockTrade>();
        private Dictionary<string, StockCandidate> _stockCandidateDict = new Dictionary<string, StockCandidate>();
        private BlockingCollection<List<StockOrder>> _stockOrderMessageQueue = new BlockingCollection<List<StockOrder>>(new ConcurrentQueue<List<StockOrder>>());
        private bool _hasStockOrder = false;

        private readonly StockTradeConfig tradeConfig;
        private readonly ITradeRepository _tradeRepository;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger _logger;
        private readonly enumEnvironmentMode _enumEnvironmentMode;
        private readonly IYuantaService _yuantaService;
        private readonly string _stockAccount;
        private readonly string _stockPassword;
        public StockTraderService(IConfiguration config, ITradeRepository tradeRepository, ICandidateRepository candidateRepository, IDateTimeService dateTimeService, ILogger logger, IYuantaService yuantaService)
        {
            tradeConfig = config.GetSection("TradeConfig").Get<StockTradeConfig>();
            string environment = config.GetValue<string>("Environment").ToUpper();
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _tradeRepository = tradeRepository;
            _candidateRepository = candidateRepository;
            _dateTimeService = dateTimeService;
            _logger = logger;
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
            _yuantaService = yuantaService;
            _stockAccount = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockAccount") : "S98875005091";
            _stockPassword = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockPassword") : "1234";
           
        }
        public async Task Trade()
        {
            try
            {
                _stockHoldingDict = (await _tradeRepository.GetStockHolding()).ToDictionary(x => x.StockCode);
                _stockCandidateDict = (await _candidateRepository.GetActiveCandidate()).ToDictionary(x => x.StockCode);
                Task.Run(() =>
                {
                    foreach (List<StockOrder> order in _stockOrderMessageQueue.GetConsumingEnumerable())
                    {
                        ProcessStockOrder(order);
                    }
                });
                objYuantaOneAPI.Open(_enumEnvironmentMode);
                await Task.Delay(-1, _cts.Token);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Trade() 已被取消。");
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                objYuantaOneAPI.LogOut();
                objYuantaOneAPI.Close();
                objYuantaOneAPI.Dispose();
            }
        }

        void objApi_OnResponse(int intMark, uint dwIndex, string strIndex, object objHandle, object objValue)
        {
            string strResult = "";
            try
            {
                switch (intMark)
                {
                    case 0: //系統回應
                        strResult = Convert.ToString(objValue);
                        _yuantaService.SystemResponseHandler(strResult, objYuantaOneAPI, _stockAccount, _stockPassword, _cts, SubscribeStockTick);
                        break;
                    case 1: //代表為RQ/RP 所回應的
                        switch (strIndex)
                        {
                            case "Login":       //一般/子帳登入
                                strResult = _yuantaService.FunAPILogin_Out((byte[])objValue);
                                break;
                            case "30.100.10.31"://現貨下單
                                strResult = _yuantaService.FunStkOrder_Out((byte[])objValue);
                                break;
                            default:           //不在表列中的直接呈現訊息
                                strResult = $"{strIndex},{objValue}";
                                break;
                        }
                        break;
                    case 2: //訂閱所回應
                        switch (strIndex)
                        {
                            case "210.10.60.10":    //訂閱五檔報價
                                strResult = _yuantaService.FunRealFivetick_Out((byte[])objValue);
                                FiveTickHandler(strResult, out string stockCode, out decimal level1AskPrice, out int level1AskSize);
                                StockOrder(stockCode, level1AskPrice, level1AskSize);
                                break;
                            default:
                                strResult = $"{strIndex},{objValue}";
                                break;
                        }
                        break;
                    default:
                        strResult = Convert.ToString(objValue);
                        break;
                }
                _logger.Information(strResult);
            }
            catch (Exception ex)
            {
                strResult = "Error: " + ex;
                _logger.Error(strResult);
            }
        }
        private void FiveTickHandler(string strResult, out string stockCode, out decimal level1AskPrice, out int level1AskSize)
        {
            stockCode = "";
            level1AskPrice = 0;
            level1AskSize = 0;
            if (string.IsNullOrEmpty(strResult)) return;
            string[] tickInfo = strResult.Split(',');
            stockCode = tickInfo[1];
            if (!decimal.TryParse(tickInfo[13], out level1AskPrice) || !int.TryParse(tickInfo[18], out level1AskSize))
            {
                _logger.Error($"The level1AskPrice or level1AskSize of Stock code {stockCode} is error");
                return;
            }
            level1AskPrice = level1AskPrice / 10000;
        }
        private void StockOrder(string stockCode, decimal level1AskPrice, int level1AskSize)
        {
            if (GetStockHoldingList().Count > 0)
            {

            }
            else
            {
                if (!_stockCandidateDict.TryGetValue(stockCode, out StockCandidate candidate)) return;
                if (level1AskPrice == 0 || level1AskSize == 0) return;
                if (level1AskPrice == candidate.EntryPoint &&
                    level1AskSize >= (int)(tradeConfig.MaxAmountPerStock / (candidate.EntryPoint * 1000)) &&
                    candidate.EntryPoint >= (candidate.Last9Close.Sum() + candidate.EntryPoint) / 10)
                {
                    StockOrder stockOrder = new StockOrder();
                    stockOrder.StkCode = stockCode;   // 股票代號
                    stockOrder.PriceFlag = "";   // 價格種類, H:漲停 -:平盤  L:跌停 " ":限價  M:市價單
                    stockOrder.BuySell = "B";   // 買賣別, B:買  S:賣
                    stockOrder.Time_in_force = "4";  // 委託效期, 0:ROD 3:IOC  4:FOK
                    stockOrder.Price = Convert.ToInt64(level1AskPrice * 10000);  // 委託價格
                    stockOrder.OrderQty = Convert.ToInt64((int)(tradeConfig.MaxAmountPerStock / (candidate.EntryPoint * 1000)));    // 委託單位數
                    SetDefaultStockOrder(stockOrder);
                    List<StockOrder> lstStockOrder = new List<StockOrder>() { stockOrder };
                    _stockOrderMessageQueue.Add(lstStockOrder);
                }

            }
        }
        private List<StockTrade> GetStockHoldingList()
        {
            return _stockHoldingDict.Values.Where(x => x.SaleDate == null).ToList();
        }
        private void SetDefaultStockOrder(StockOrder stockOrder)
        {
            stockOrder.Identify = Convert.ToInt32(this.txtIdentify.Text.Trim()); // 識別碼
            stockOrder.Account = _stockAccount; // 現貨帳號
            stockOrder.APCode = Convert.ToInt16(0);    // 交易市場別, 0:一般 2:盤後零股 4:盤中零股 7:盤後
            stockOrder.TradeKind = Convert.ToInt16(0);  // 交易性質, 0:委託單 3:改量 4:取消 7:改價
            stockOrder.OrderType = "0";   // 委託種類, 0:現貨 3:融資 4:融券 5借券(賣出) 6:借券(賣出) 9:現股當沖
            stockOrder.SellerNo = Convert.ToInt16(this.txtSellerNo.Text.Trim());    // 營業員代碼
            stockOrder.OrderNo = this.txtOrderNo.Text.Trim();   // 委託書編號
            stockOrder.TradeDate = _dateTimeService.GetTaiwanTime().ToString("yyyy/MM/dd");
            stockOrder.BasketNo = this.txtBasketNo.Text.Trim(); // BasketNo


            //RQRP
            
        }
        private void ProcessStockOrder(List<StockOrder> stockOrder)
        {
            if (!_hasStockOrder && GetStockHoldingList().Count == 0)
            {
                bool bResult = objYuantaOneAPI.SendStockOrder(_stockAccount, stockOrder);
                _hasStockOrder = true;
            }
        }
        private void SubscribeStockTick()
        {
            List<FiveTickA> lstFiveTick = new List<FiveTickA>();
            lstFiveTick.AddRange(_stockHoldingDict.Select(x => new FiveTickA
            {
                MarketNo = Convert.ToByte(x.Value.Market),
                StockCode = x.Value.StockCode,
            }));
            lstFiveTick.AddRange(_stockCandidateDict.Select(x => new FiveTickA
            {
                MarketNo = Convert.ToByte(x.Value.Market),
                StockCode = x.Value.StockCode
            }));
            lstFiveTick = lstFiveTick.GroupBy(x => x.StockCode).Select(g => g.First()).ToList();
            objYuantaOneAPI.SubscribeFiveTickA(lstFiveTick);
        }
    }
}
