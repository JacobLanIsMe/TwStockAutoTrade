using Core.Enum;
using Core.Model;
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
    public class FutureTraderService : IFutureTraderService
    {
        YuantaOneAPITrader objYuantaOneAPI = new YuantaOneAPITrader();
        private readonly enumEnvironmentMode _enumEnvironmentMode;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private ConcurrentBag<int> _first5MinuteTickBag = new ConcurrentBag<int>();
        private BlockingQueue<FutureOrder> _futureOrderMessageQueue;
        private int _first5MinuteHigh = 0;
        private int _first5MinuteLow = 0;
        private int _longProfitPoint = 0;
        private int _longStopLossPoint = 0;
        private int _shortProfitPoint = 0;
        private int _shortStopLossPoint = 0;
        private bool _hasLongOrder = false;
        private bool _hasLongContract = false;
        private bool _hasShortOrder = false;
        private bool _hasShortContract = false;
        private readonly TimeSpan _marketOpenTime = new TimeSpan(8, 45, 0);
        private readonly TimeSpan _afterMarketOpen5Minute;
        private readonly ILogger _logger;
        private readonly string _futureAccount;
        private readonly string _futurePassword;
        private readonly string _targetFutureCode;
        private readonly int _maxOrderQuantity;
        private readonly int _profitPoint;
        private readonly int _stopLossPoint;
        private readonly IYuantaService _yuantaService;
        private readonly DateTime _now;
        private readonly string _targetCommodityId;
        private readonly string _settlementMonth;
        private readonly Dictionary<string, string> _codeCommodityIdDict = new Dictionary<string, string>
        {
            { "TXF1", "FITX" },
            { "MXF1", "FIMTX" },
            { "TMF0", "FITM" },
            { "MXF8", "FIMTX" }
        };
        public FutureTraderService(IConfiguration config, ILogger logger, IDateTimeService dateTimeService, IYuantaService yuantaService)
        {
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
            string environment = config.GetValue<string>("Environment").ToUpper();
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _logger = logger;
            _futureAccount = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("FutureAccount", EnvironmentVariableTarget.Machine) : "S98875005091";
            _futurePassword = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockPassword", EnvironmentVariableTarget.Machine) : "1234";
            _targetFutureCode = config.GetValue<string>("TargetFutureCode");
            _maxOrderQuantity = config.GetValue<int>("MaxOrderQuantity");
            _profitPoint = config.GetValue<int>("ProfitPoint");
            _stopLossPoint = config.GetValue<int>("StopLossPoint");
            _afterMarketOpen5Minute = _marketOpenTime.Add(TimeSpan.FromMinutes(5));
            _yuantaService = yuantaService;
            _futureOrderMessageQueue = new BlockingQueue<FutureOrder>(ProcessFutureOrder);
            if (!_codeCommodityIdDict.TryGetValue(_targetFutureCode, out _targetCommodityId)) throw new Exception($"Can't find commodityId by code {_targetFutureCode}");
            _now = dateTimeService.GetTaiwanTime();
            DateTime thirdWednesday = GetThirdWednesday(_now.Year, _now.Month);
            _settlementMonth = _now.Day > thirdWednesday.Day ? _now.AddMonths(1).ToString("yyyyMM") : _now.ToString("yyyyMM");
        }
        public async Task Trade()
        {
            try
            {
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
                        _yuantaService.SystemResponseHandler(strResult, objYuantaOneAPI, _futureAccount, _futurePassword, _cts, SubscribeFutureTick);
                        break;
                    case 1: //代表為RQ/RP 所回應的
                        switch (strIndex)
                        {
                            case "Login":       //一般/子帳登入
                                strResult = _yuantaService.FunAPILogin_Out((byte[])objValue);
                                break;
                            case "30.100.20.24"://期貨下單(新)
                                strResult = _yuantaService.FunFutOrder_Out((byte[])objValue);
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
                                //RealReportHandler(strResult);
                                break;
                            case "210.10.40.10":    //訂閱個股分時明細
                                strResult = _yuantaService.FunRealStocktick_Out((byte[])objValue);
                                TickHandler(strResult, out int tickPrice);
                                //FutureOrder(tickPrice);
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
        
        private void TickHandler(string strResult, out int tickPrice)
        {
            tickPrice = 0;
            if (string.IsNullOrEmpty(strResult)) return;
            string[] tickInfo = strResult.Split(',');
            if (!TimeSpan.TryParse(tickInfo[3], out TimeSpan tickTime))
            {
                _logger.Error("Tick time failed in TickHandler");
                return;
            }
            if (!int.TryParse(tickInfo[6], out tickPrice))
            {
                _logger.Error("Tick price failed in TickHandler");
                return;
            }
            tickPrice = tickPrice / 1000;
            if (tickTime < _afterMarketOpen5Minute && tickTime >= _marketOpenTime)
            {
                _first5MinuteTickBag.Add(tickPrice);
            }
            if (_first5MinuteHigh == 0 && _first5MinuteLow == 0 && tickTime >= _afterMarketOpen5Minute)
            {
                _first5MinuteHigh = _first5MinuteTickBag.Max();
                _longProfitPoint = _first5MinuteHigh + _profitPoint;
                _longStopLossPoint = _first5MinuteHigh - _stopLossPoint;
                _first5MinuteLow = _first5MinuteTickBag.Min();
                _shortProfitPoint = _first5MinuteLow - _profitPoint;
                _shortStopLossPoint = _first5MinuteLow + _stopLossPoint;
            }
        }
        private FutureOrder SetDefaultFutureOrder()
        {
            FutureOrder futureOrder = new FutureOrder();
            futureOrder.Identify = 001;                                                     //識別碼
            futureOrder.Account = _futureAccount;                                           //期貨帳號
            futureOrder.FunctionCode = 0;                                                   //功能別, 0:委託單 4:取消 5:改量 7:改價                     
            futureOrder.CommodityID1 = _targetCommodityId;                                  //商品名稱1
            futureOrder.CallPut1 = "";                                                      //買賣權1
            futureOrder.SettlementMonth1 = Convert.ToInt32(_settlementMonth);               //商品年月1
            futureOrder.StrikePrice1 = 0;                                                   //屐約價1
            futureOrder.OrderQty1 = Convert.ToInt16(_maxOrderQuantity);                     //委託口數1
            #region 組合單應填欄位
            futureOrder.CommodityID2 = "";                                                  //商品名稱2
            futureOrder.CallPut2 = "";                                                      //買賣權2
            futureOrder.SettlementMonth2 = 0;                                               //商品年月2
            futureOrder.StrikePrice2 = 0;                                                   //屐約價2                 
            futureOrder.OrderQty2 = 0;                                                      //委託口數2
            futureOrder.BuySell2 = "";                                                      //買賣別2
            #endregion
            futureOrder.OpenOffsetKind = "2";                                               //新平倉碼, 0:新倉 1:平倉 2:自動                                          
            futureOrder.DayTradeID = "Y";                                                   //當沖註記, Y:當沖  "":非當沖
            futureOrder.SellerNo = 0;                                                       //營業員代碼                                            
            futureOrder.OrderNo = "";                                                       //委託書編號           
            futureOrder.TradeDate = _now.ToString("yyyy/MM/dd");                            //交易日期                            
            futureOrder.BasketNo = "";                                                      //BasketNo
            futureOrder.Session = "";                                                       //通路種類, 1:預約 "":盤中單
            return futureOrder;                                                                             
        }
        private void FutureOrder(int tickPrice)
        {
            if (tickPrice == 0) return;
            if (!_hasLongOrder && !_hasLongContract && tickPrice > _first5MinuteHigh && tickPrice < _first5MinuteHigh + 5)
            {
                FutureOrder futureOrder = SetDefaultFutureOrder();
                futureOrder.Price = _first5MinuteHigh * 10000;                    //委託價格
                futureOrder.BuySell1 = EBuySellType.B.ToString();                 //買賣別, "B":買 "S":賣
                futureOrder.OrderType = ((int)EFutureOrderType.限價).ToString();   //委託方式, 1:市價 2:限價 3:範圍市價
                futureOrder.OrderCond = "";                                       //委託條件, "":ROD 1:FOK 2:IOC
                _futureOrderMessageQueue.Add(futureOrder);
            }
            else if (_hasLongContract && (tickPrice <= _longStopLossPoint || tickPrice >= _longProfitPoint))
            {
                FutureOrder futureOrder = SetDefaultFutureOrder();
                futureOrder.Price = tickPrice * 10000;
                futureOrder.BuySell1 = EBuySellType.S.ToString();
                futureOrder.OrderType = ((int)EFutureOrderType.市價).ToString();
                futureOrder.OrderCond = "";
                _futureOrderMessageQueue.Add(futureOrder);
            }
            else if (!_hasShortOrder && !_hasShortContract && tickPrice < _first5MinuteLow && tickPrice > _first5MinuteLow - 5)
            {
                FutureOrder futureOrder = SetDefaultFutureOrder();
                futureOrder.Price = _first5MinuteLow * 10000;                     //委託價格
                futureOrder.BuySell1 = EBuySellType.S.ToString();                 //買賣別, "B":買 "S":賣
                futureOrder.OrderType = ((int)EFutureOrderType.限價).ToString();   //委託方式, 1:市價 2:限價 3:範圍市價
                futureOrder.OrderCond = "";                                       //委託條件, "":ROD 1:FOK 2:IOC
                _futureOrderMessageQueue.Add(futureOrder);
            }
            else if (_hasShortContract && (tickPrice >= _shortStopLossPoint || tickPrice <= _shortProfitPoint))
            {
                FutureOrder futureOrder = SetDefaultFutureOrder();
                futureOrder.Price = tickPrice * 10000;
                futureOrder.BuySell1 = EBuySellType.B.ToString();
                futureOrder.OrderType = ((int)EFutureOrderType.市價).ToString();
                futureOrder.OrderCond = "";
                _futureOrderMessageQueue.Add(futureOrder);
            }
            else
            {
                return;
            }
        }
        private void ProcessFutureOrder(FutureOrder futureOrder)
        {
            if (!_hasLongOrder && !_hasLongContract && futureOrder.BuySell1 == EBuySellType.B.ToString() && futureOrder.OrderType == ((int)EFutureOrderType.限價).ToString())
            {

            }
            bool bResult = objYuantaOneAPI.SendFutureOrder(_futureAccount, new List<FutureOrder>() { futureOrder });
        }
        private void SubscribeFutureTick()
        {
            List<StockTick> lstStocktick = new List<StockTick>();
            StockTick stocktick = new StockTick();
            stocktick.MarketNo = Convert.ToByte(enumMarketType.TAIFEX);      //填入查詢市場代碼
            stocktick.StockCode = _targetFutureCode;
            lstStocktick.Add(stocktick);
            objYuantaOneAPI.SubscribeStockTick(lstStocktick);
        }
        
        
        private DateTime GetThirdWednesday(int year, int month)
        {
            // 取得該月的第一天
            DateTime firstDayOfMonth = new DateTime(year, month, 1);

            // 計算該月第一個禮拜三的日期
            int daysUntilWednesday = ((int)DayOfWeek.Wednesday - (int)firstDayOfMonth.DayOfWeek + 7) % 7;
            DateTime firstWednesday = firstDayOfMonth.AddDays(daysUntilWednesday);

            // 第三個禮拜三 = 第一個禮拜三 + 2 週
            DateTime thirdWednesday = firstWednesday.AddDays(14);

            return thirdWednesday;
        }
    }
}
