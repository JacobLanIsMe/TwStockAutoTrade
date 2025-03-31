using Core.Enum;
using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Service
{
    public class StockTraderService : IStockTraderService
    {
        YuantaOneAPITrader objYuantaOneAPI = new YuantaOneAPITrader();
        private Dictionary<string, StockCandidate> _stockCandidateDict = new Dictionary<string, StockCandidate>(); // Key: StockCode
        private CancellationTokenSource _cts;
        private Trade _trade = null;
        private readonly StockTradeConfig tradeConfig;
        private readonly ICandidateRepository _candidateRepository;
        private readonly ILogger _logger;
        private readonly enumEnvironmentMode _enumEnvironmentMode;
        private readonly IYuantaService _yuantaService;
        private readonly string _stockAccount;
        private readonly string _stockPassword;
        private readonly string _todayDate;
        public StockTraderService(IConfiguration config, ICandidateRepository candidateRepository, IDateTimeService dateTimeService, ILogger logger, IYuantaService yuantaService)
        {
            tradeConfig = config.GetSection("TradeConfig").Get<StockTradeConfig>();
            string environment = config.GetValue<string>("Environment").ToUpper();
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _candidateRepository = candidateRepository;
            DateTime now = dateTimeService.GetTaiwanTime();
            DateTime stopTime = now.Date.AddHours(13).AddMinutes(25);
            TimeSpan delayTime = stopTime - now;
            _cts = delayTime > TimeSpan.Zero ? new CancellationTokenSource(delayTime) : new CancellationTokenSource();
            _todayDate = now.ToString("yyyy/MM/dd");
            _logger = logger;
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
            _yuantaService = yuantaService;
            _stockAccount = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockAccount", EnvironmentVariableTarget.Machine) : "S98875005091";
            _stockPassword = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockPassword", EnvironmentVariableTarget.Machine) : "1234";
        }
        public async Task Trade()
        {
            try
            {
                List<StockCandidate> stockCandidateList = await _candidateRepository.GetActiveCandidate();
                if (!stockCandidateList.Any()) return;
                SetLast9Close(stockCandidateList);
                _stockCandidateDict = stockCandidateList.ToDictionary(x => x.StockCode);
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
                await _candidateRepository.UpdateHoldingStock(_stockCandidateDict.Values.ToList());
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
                        _yuantaService.SystemResponseHandler(strResult, objYuantaOneAPI, _stockAccount, _stockPassword, _cts, Subscribe);
                        break;
                    case 1: //代表為RQ/RP 所回應的
                        switch (strIndex)
                        {
                            case "Login":       //一般/子帳登入
                                strResult = _yuantaService.FunAPILogin_Out((byte[])objValue);
                                break;
                            case "30.100.10.31"://現貨下單
                                strResult = _yuantaService.FunStkOrder_Out((byte[])objValue);
                                StockOrderHandler(strResult);
                                break;
                            default:           //不在表列中的直接呈現訊息
                                strResult = $"{strIndex},{objValue}";
                                break;
                        }
                        break;
                    case 2: //訂閱所回應
                        switch (strIndex)
                        {
                            case "200.10.10.26":    //逐筆即時回報
                                strResult = _yuantaService.FunRealReport_Out((byte[])objValue);
                                RealReportHandler(strResult);
                                break;
                            case "210.10.70.11":    //Watchlist報價表(指定欄位)
                                strResult = _yuantaService.FunRealWatchlist_Out((byte[])objValue);
                                WatchListHandler(strResult);
                                break;
                            case "210.10.60.10":    //訂閱五檔報價
                                string fivetickResult = _yuantaService.FunRealFivetick_Out((byte[])objValue);
                                FiveTickHandler(fivetickResult, out string stockCode, out decimal level1AskPrice, out int level1AskSize);
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
                if (!string.IsNullOrEmpty(strResult))
                {
                    _logger.Information(strResult);
                }
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
        private StockOrder SetDefaultStockOrder()
        {
            StockOrder stockOrder = new StockOrder();
            stockOrder.Identify = Convert.ToInt32(001); // 識別碼
            stockOrder.Account = _stockAccount; // 現貨帳號
            stockOrder.APCode = Convert.ToInt16(0);    // 交易市場別, 0:一般 2:盤後零股 4:盤中零股 7:盤後
            stockOrder.TradeKind = Convert.ToInt16(0);  // 交易性質, 0:委託單 3:改量 4:取消 7:改價
            stockOrder.OrderType = "0";   // 委託種類, 0:現貨 3:融資 4:融券 5借券(賣出) 6:借券(賣出) 9:現股當沖
            stockOrder.TradeDate = _todayDate;
            stockOrder.SellerNo = Convert.ToInt16(0); // Convert.ToInt16(this.txtSellerNo.Text.Trim());    // 營業員代碼
            stockOrder.OrderNo = ""; // this.txtOrderNo.Text.Trim();   // 委託書編號
            stockOrder.BasketNo = ""; // this.txtBasketNo.Text.Trim(); // BasketNo
            return stockOrder;
        }
        private void StockOrder(string stockCode, decimal level1AskPrice, int level1AskSize)
        {
            if (string.IsNullOrEmpty(stockCode) || level1AskPrice == 0 || level1AskSize == 0 || _trade != null) return;
            if (!_stockCandidateDict.TryGetValue(stockCode, out StockCandidate candidate) || !candidate.IsTradingStarted) return;
            if (candidate.IsHolding)
            {
                if ((level1AskPrice <= candidate.EntryPoint && level1AskPrice <= candidate.StopLossPoint) ||
                    (level1AskPrice > candidate.EntryPoint && level1AskPrice < (candidate.SumOfLast9Close + level1AskPrice) / 10))
                {
                    StockOrder stockOrder = SetDefaultStockOrder();
                    stockOrder.StkCode = stockCode;
                    stockOrder.PriceFlag = "M";
                    stockOrder.BuySell = EBuySellType.S.ToString();
                    stockOrder.Time_in_force = "0";
                    stockOrder.Price = Convert.ToInt64(0);
                    stockOrder.OrderQty = candidate.PurchasedLot;
                    ProcessStockOrder(stockOrder);
                }
            }
            else
            {
                int orderQty = (int)(tradeConfig.MaxAmountPerStock / (candidate.EntryPoint * 1000));
                if (level1AskPrice == candidate.EntryPoint &&
                    orderQty > 0 &&
                    level1AskSize >= orderQty &&
                    candidate.EntryPoint >= (candidate.SumOfLast9Close + candidate.EntryPoint) / 10 &&
                    tradeConfig.MaxStockCount > _stockCandidateDict.Count(x => x.Value.IsHolding))
                {
                    StockOrder stockOrder = SetDefaultStockOrder();
                    stockOrder.StkCode = stockCode;   // 股票代號
                    stockOrder.PriceFlag = "";   // 價格種類, H:漲停 -:平盤  L:跌停 " ":限價  M:市價單
                    stockOrder.BuySell = EBuySellType.B.ToString();   // 買賣別, B:買  S:賣
                    stockOrder.Time_in_force = "4";  // 委託效期, 0:ROD 3:IOC  4:FOK
                    stockOrder.Price = Convert.ToInt64(level1AskPrice * 10000);  // 委託價格
                    stockOrder.OrderQty = Convert.ToInt64(orderQty);    // 委託單位數
                    ProcessStockOrder(stockOrder);
                }
            }
        }
        private void ProcessStockOrder(StockOrder stockOrder)
        {
            bool bResult = objYuantaOneAPI.SendStockOrder(_stockAccount, new List<StockOrder>() { stockOrder });
            if (bResult)
            {
                _trade = new Trade();
            }
            else
            {
                _logger.Error($"SendStockOrder error. Stock code: {stockOrder.StkCode}, Buy or Sell: {stockOrder.BuySell}");
            }
        }
        private void StockOrderHandler(string strResult)
        {
            string[] resultArray = strResult.Split(',');
            if (string.IsNullOrEmpty(resultArray[4].Trim()) ||
                !DateTime.TryParse(resultArray[5], out DateTime orderTime) ||
                !string.IsNullOrEmpty(resultArray[resultArray.Length - 2].Trim()) ||
                !string.IsNullOrEmpty(resultArray[resultArray.Length - 1].Trim()))
            {
                _logger.Error($"SendStockOrder error. Error message: {resultArray[resultArray.Length - 2]}, {resultArray[resultArray.Length - 1]}");
                _trade = null;
            }
        }
        private void RealReportHandler(string strResult)
        {
            string[] reportArray = strResult.Split(',');
            if (!int.TryParse(reportArray[1].Split(':')[1], out int reportType))
            {
                _logger.Error("Report type error");
            }
            if (!int.TryParse(reportArray[13], out int purchasedShare))
            {
                _logger.Error("PurchasedLot error");
            }
            purchasedShare = purchasedShare / 1000;
            string orderNo = reportArray[2].Trim().Substring(4); // 委託單號
            if (reportType == 50)
            {
                string errorCode = reportArray[reportArray.Length - 1];
                if (errorCode == "13048" || errorCode == "19348")
                {
                    _trade = null;
                }
                else
                {
                    _trade.OrderNo = orderNo;
                    _trade.OrderedLot = purchasedShare;
                }
            }
            else if (reportType == 51)
            {
                string stockCode = reportArray[4].Trim();
                if (!_stockCandidateDict.TryGetValue(stockCode, out StockCandidate candidate)) return;
                if (orderNo != _trade.OrderNo) return;
                if (reportArray[9] == EBuySellType.B.ToString())
                {
                    candidate.IsHolding = true;
                    candidate.PurchasedLot = candidate.PurchasedLot + purchasedShare;
                    if (candidate.PurchasedLot == _trade.OrderedLot)
                    {
                        _trade = null;
                    }
                }
                else
                {
                    candidate.PurchasedLot = candidate.PurchasedLot - purchasedShare;
                    if (candidate.PurchasedLot == 0)
                    {
                        candidate.IsHolding = false;
                        _trade = null;
                    }
                }
            }
        }
        private void WatchListHandler(string strResult)
        {
            string[] watchListResult = strResult.Split(',');
            string stockCode = watchListResult[1];
            if (!decimal.TryParse(watchListResult[3], out decimal tradePrice)) return;
            if (!_stockCandidateDict.TryGetValue(stockCode, out StockCandidate candidate)) return;
            candidate.IsTradingStarted = true;
            UnsubscribeWatchlist(candidate.Market, candidate.StockCode);
        }
        private void Subscribe()
        {
            List<FiveTickA> lstFiveTick = new List<FiveTickA>();
            List<Watchlist> lstWatchlist = new List<Watchlist>();
            foreach (var i in _stockCandidateDict)
            {
                FiveTickA fiveTickA = new FiveTickA();
                fiveTickA.MarketNo = Convert.ToByte(i.Value.Market);
                fiveTickA.StockCode = i.Value.StockCode;
                lstFiveTick.Add(fiveTickA);
                Watchlist watch = new Watchlist();
                watch.IndexFlag = Convert.ToByte(7);    //填入訂閱索引值, 7: 成交價
                watch.MarketNo = Convert.ToByte(i.Value.Market);      //填入查詢市場代碼
                watch.StockCode = i.Value.StockCode;
                lstWatchlist.Add(watch);
            }
            objYuantaOneAPI.SubscribeFiveTickA(lstFiveTick);
            objYuantaOneAPI.SubscribeWatchlist(lstWatchlist);
        }
        private void UnsubscribeWatchlist(enumMarketType enumMarketNo, string stockCode)
        {
            Watchlist watch = new Watchlist();
            watch.IndexFlag = Convert.ToByte(7);    //填入定閱索引值, 7: 成交價
            watch.MarketNo = Convert.ToByte(enumMarketNo);      //填入查詢市場代碼
            watch.StockCode = stockCode;                     //填入查詢股票代碼
            objYuantaOneAPI.UnsubscribeWatchlist(new List<Watchlist>() { watch });
        }
        private void SetLast9Close(List<StockCandidate> stockCandidateList)
        {
            foreach (var i in stockCandidateList)
            {
                if (string.IsNullOrEmpty(i.Last9TechData)) throw new Exception($"The Last9TechData of Stock {i.StockCode} is null.");
                List<StockTechData> last9TechDataList = JsonConvert.DeserializeObject<List<StockTechData>>(i.Last9TechData);
                if (last9TechDataList.Count != 9) throw new Exception($"The count of Last9TechData of Stock {i.StockCode} is not 9.");
                i.SumOfLast9Close = last9TechDataList.Sum(x => x.Close);
            }
        }
    }
}
