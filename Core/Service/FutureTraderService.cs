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
        private ConcurrentBag<int> _first15MinuteTickBag = new ConcurrentBag<int>();
        private ConcurrentBag<FutureTrade> tradeList = new ConcurrentBag<FutureTrade>();
        private int _first15MinuteHigh = 0;
        private int _first15MinuteLow = 0;
        private int _longProfitPoint = 0;
        private int _longStopLossPoint = 0;
        private int _shortProfitPoint = 0;
        private int _shortStopLossPoint = 0;
        private string _orderNo = "";
        private bool _hasLongContract = false;
        private bool _hasShortContract = false;
        private bool _hitProfitPoint = false;
        private readonly TimeSpan _afterMarketOpen15Minute;
        private readonly TimeSpan _beforeMarketClose10Minute;
        private readonly TimeSpan _lastEntryTime;
        private readonly TimeSpan _eveningMarketCloseTime = TimeSpan.FromHours(5);
        private readonly ILogger _logger;
        private readonly string _futureAccount;
        private readonly string _futurePassword;
        private readonly FutureConfig _targetFutureConfig;
        private readonly int _maxOrderQuantity;
        private readonly int _profitPoint;
        private readonly int _stopLossPoint;
        private readonly IYuantaService _yuantaService;
        private readonly IDateTimeService _dateTimeService;
        private readonly string _settlementMonth;
        private readonly string _defaultOrderNo = "defaultOrderNo";
        public FutureTraderService(IConfiguration config, ILogger logger, IDateTimeService dateTimeService, IYuantaService yuantaService)
        {
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
            string environment = config.GetValue<string>("Environment").ToUpper();
            _yuantaService = yuantaService;
            _dateTimeService = dateTimeService;
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _logger = logger;
            _futureAccount = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("FutureAccount", EnvironmentVariableTarget.Machine) : "S98875005091";
            _futurePassword = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockPassword", EnvironmentVariableTarget.Machine) : "1234";
            _maxOrderQuantity = config.GetValue<int>("MaxOrderQuantity");
            _profitPoint = config.GetValue<int>("ProfitPoint");
            _stopLossPoint = config.GetValue<int>("StopLossPoint");
            _settlementMonth = GetSettlementMonth();
            _targetFutureConfig = SetFutureConfig(config);
            _afterMarketOpen15Minute = _targetFutureConfig.MarketOpenTime.Add(TimeSpan.FromMinutes(15));
            _beforeMarketClose10Minute = _targetFutureConfig.MarketCloseTime.Subtract(TimeSpan.FromMinutes(10));
            _lastEntryTime = _targetFutureConfig.MarketCloseTime.Subtract(TimeSpan.FromHours(1)).Subtract(TimeSpan.FromMinutes(30));
            _first15MinuteHigh = config.GetValue<int>("First15MinuteHigh");
            _first15MinuteLow = config.GetValue<int>("First15MinuteLow");
            SetExitPoint();
            TimeSpan nowTimeSpan = _dateTimeService.GetTaiwanTime().TimeOfDay;
            if (nowTimeSpan < _eveningMarketCloseTime)
            {
                nowTimeSpan = nowTimeSpan.Add(TimeSpan.FromHours(24));
            }
            if (nowTimeSpan >= _targetFutureConfig.MarketOpenTime && (_first15MinuteHigh == 0 || _first15MinuteLow == 0)) throw new Exception("The first 15 minute high and low can not be 0");
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
                                FutureOrderHandler(strResult);
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
                            case "210.10.40.10":    //訂閱個股分時明細
                                strResult = _yuantaService.FunRealStocktick_Out((byte[])objValue);
                                TickHandler(strResult, out TimeSpan tickTime, out int tickPrice);
                                FutureOrder(tickTime, tickPrice);
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
        private void TickHandler(string strResult, out TimeSpan tickTime, out int tickPrice)
        {
            tickPrice = 0;
            tickTime = TimeSpan.Zero;
            if (string.IsNullOrEmpty(strResult)) return;
            string[] tickInfo = strResult.Split(',');
            if (!TimeSpan.TryParse(tickInfo[3], out tickTime))
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
            if (tickTime < _eveningMarketCloseTime)
            {
                tickTime = tickTime.Add(TimeSpan.FromHours(24));
            }
            if (tickTime < _afterMarketOpen15Minute && tickTime >= _targetFutureConfig.MarketOpenTime)
            {
                _first15MinuteTickBag.Add(tickPrice);
            }
            if ((_first15MinuteHigh == 0 || _first15MinuteLow == 0) && tickTime >= _afterMarketOpen15Minute)
            {
                _first15MinuteHigh = _first15MinuteTickBag.Max();
                _first15MinuteLow = _first15MinuteTickBag.Min();
                SetExitPoint();
            }
            if (!_hitProfitPoint && ((_longProfitPoint != 0 && tickPrice > _longProfitPoint) || (_shortProfitPoint != 0 && tickPrice < _shortProfitPoint)))
            {
                _hitProfitPoint = true;
            }
            if (string.IsNullOrEmpty(_orderNo) && !_hasLongContract && !_hasShortContract && (tickTime >= _lastEntryTime || _hitProfitPoint))
            {
                _cts.Cancel();
            }
        }
        private FutureOrder SetDefaultFutureOrder()
        {
            FutureOrder futureOrder = new FutureOrder();
            futureOrder.Identify = 001;                                                      //識別碼
            futureOrder.Account = _futureAccount;                                            //期貨帳號
            futureOrder.FunctionCode = 0;                                                    //功能別, 0:委託單 4:取消 5:改量 7:改價                     
            futureOrder.CommodityID1 = _targetFutureConfig.CommodityId;                      //商品名稱1
            futureOrder.CallPut1 = "";                                                       //買賣權1
            futureOrder.SettlementMonth1 = Convert.ToInt32(_settlementMonth);                //商品年月1
            futureOrder.StrikePrice1 = 0;                                                    //屐約價1
            futureOrder.OrderQty1 = Convert.ToInt16(_maxOrderQuantity);                      //委託口數1
            futureOrder.OpenOffsetKind = "2";                                                //新平倉碼, 0:新倉 1:平倉 2:自動                                          
            futureOrder.OrderCond = "";                                                      //委託條件, "":ROD 1:FOK 2:IOC
            futureOrder.DayTradeID = "Y";                                                    //當沖註記, Y:當沖  "":非當沖
            futureOrder.SellerNo = 0;                                                        //營業員代碼                                            
            futureOrder.OrderNo = "";                                                        //委託書編號           
            futureOrder.TradeDate = _dateTimeService.GetTaiwanTime().ToString("yyyy/MM/dd"); //交易日期                            
            futureOrder.BasketNo = "";                                                       //BasketNo
            futureOrder.Session = "";                                                        //通路種類, 1:預約 "":盤中單
            #region 組合單應填欄位
            futureOrder.CommodityID2 = "";                                                   //商品名稱2
            futureOrder.CallPut2 = "";                                                       //買賣權2
            futureOrder.SettlementMonth2 = 0;                                                //商品年月2
            futureOrder.StrikePrice2 = 0;                                                    //屐約價2                 
            futureOrder.OrderQty2 = 0;                                                       //委託口數2
            futureOrder.BuySell2 = "";                                                       //買賣別2
            #endregion
            return futureOrder;
        }
        private void FutureOrder(TimeSpan tickTime, int tickPrice)
        {
            if (tickTime == TimeSpan.Zero || tickPrice == 0) return;
            if (_first15MinuteHigh == 0 || _first15MinuteLow == 0 ||
                _longProfitPoint == 0 || _longStopLossPoint == 0 ||
                _shortProfitPoint == 0 || _shortStopLossPoint == 0) return;
            if (string.IsNullOrEmpty(_orderNo))
            {
                if (_hasLongContract)
                {
                    if (tickPrice > _longProfitPoint || tickPrice < _longStopLossPoint || tickTime >= _beforeMarketClose10Minute)
                    {
                        FutureOrder futureOrder = SetDefaultFutureOrder();
                        futureOrder.Price = tickPrice * 10000;                               //委託價格
                        futureOrder.BuySell1 = EBuySellType.S.ToString();                   //買賣別, "B":買 "S":賣
                        futureOrder.OrderType = ((int)EFutureOrderType.市價).ToString();     //委託方式, 1:市價 2:限價 3:範圍市價
                        ProcessFutureOrder(futureOrder);
                    }
                }
                else if (_hasShortContract)
                {
                    if (tickPrice < _shortProfitPoint && tickPrice > _shortStopLossPoint || tickTime >= _beforeMarketClose10Minute)
                    {
                        FutureOrder futureOrder = SetDefaultFutureOrder();
                        futureOrder.Price = tickPrice * 10000;
                        futureOrder.BuySell1 = EBuySellType.B.ToString();
                        futureOrder.OrderType = ((int)EFutureOrderType.市價).ToString();
                        ProcessFutureOrder(futureOrder);
                    }
                }
                else if (tickTime < _lastEntryTime)
                {
                    if (tickPrice > _first15MinuteHigh + 10)
                    {
                        FutureOrder futureOrder = SetDefaultFutureOrder();
                        futureOrder.Price = _first15MinuteHigh * 10000;                     //委託價格
                        futureOrder.BuySell1 = EBuySellType.B.ToString();                 //買賣別, "B":買 "S":賣
                        futureOrder.OrderType = ((int)EFutureOrderType.限價).ToString();   //委託方式, 1:市價 2:限價 3:範圍市價
                        ProcessFutureOrder(futureOrder);
                    }
                    else if (tickPrice < _first15MinuteLow - 10)
                    {
                        FutureOrder futureOrder = SetDefaultFutureOrder();
                        futureOrder.Price = _first15MinuteLow * 10000;                      //委託價格
                        futureOrder.BuySell1 = EBuySellType.S.ToString();                 //買賣別, "B":買 "S":賣
                        futureOrder.OrderType = ((int)EFutureOrderType.限價).ToString();   //委託方式, 1:市價 2:限價 3:範圍市價
                        ProcessFutureOrder(futureOrder);
                    }
                }
            }
            else
            {
                if (_orderNo != _defaultOrderNo && (tickTime >= _lastEntryTime || _hitProfitPoint))
                {
                    FutureOrder futureOrder = SetDefaultFutureOrder();
                    futureOrder.Price = 0;
                    futureOrder.BuySell1 = "";
                    futureOrder.OrderType = "";
                    futureOrder.FunctionCode = 4;                                         //功能別, 0:委託單 4:取消 5:改量 7:改價
                    futureOrder.OrderNo = _orderNo;                                                       //委託書編號    
                    ProcessFutureOrder(futureOrder);
                }
            }
        }
        private void ProcessFutureOrder(FutureOrder futureOrder)
        {
            bool bResult = objYuantaOneAPI.SendFutureOrder(_futureAccount, new List<FutureOrder>() { futureOrder });
            if (bResult)
            {
                _orderNo = _defaultOrderNo;
            }
            else
            {
                _logger.Error($"SendFutureOrder error. HasLongContract: {_hasLongContract}, HasShortContract: {_hasShortContract}, Buy or Sell: {futureOrder.BuySell1}");
            }
        }
        private void FutureOrderHandler(string strResult)
        {
            string[] resultArray = strResult.Split(',');
            string orderNo = resultArray[4].Trim();
            if (string.IsNullOrEmpty(orderNo) ||
                !DateTime.TryParse(resultArray[5], out DateTime orderTime) ||
                !string.IsNullOrEmpty(resultArray[resultArray.Length - 2].Trim()) ||
                !string.IsNullOrEmpty(resultArray[resultArray.Length - 1].Trim()))
            {
                _logger.Error($"FutureStockOrder error. Error message: {resultArray[resultArray.Length - 2]}, {resultArray[resultArray.Length - 1]}");
                _orderNo = "";
            }
            else
            {
                _orderNo = orderNo;
            }
        }
        private void RealReportHandler(string strResult)
        {
            //string[] reportArray = strResult.Split(',');
            //if (!int.TryParse(reportArray[1].Split(':')[1], out int reportType))
            //{
            //    _logger.Error("Report type error");
            //}
            //if (reportType != 51) return;

        }
        private void SubscribeFutureTick()
        {
            List<StockTick> lstStocktick = new List<StockTick>();
            StockTick stocktick = new StockTick();
            stocktick.MarketNo = Convert.ToByte(enumMarketType.TAIFEX);      //填入查詢市場代碼
            stocktick.StockCode = _targetFutureConfig.FutureCode;
            lstStocktick.Add(stocktick);
            objYuantaOneAPI.SubscribeStockTick(lstStocktick);
        }
        private void SetExitPoint()
        {
            if (_first15MinuteHigh == 0 || _first15MinuteLow == 0) return;
            _longProfitPoint = _first15MinuteHigh + _profitPoint;
            _longStopLossPoint = _first15MinuteHigh - _stopLossPoint;
            _shortProfitPoint = _first15MinuteLow - _profitPoint;
            _shortStopLossPoint = _first15MinuteLow + _stopLossPoint;
        }
        private FutureConfig SetFutureConfig(IConfiguration config)
        {
            DateTime now = _dateTimeService.GetTaiwanTime();
            List<FutureConfig> futureConfigList = config.GetSection("TargetFuture").Get<List<FutureConfig>>();
            FutureConfig futureConfig = new FutureConfig();
            if (now.Hour >= 5 && now.Hour < 14)
            {
                futureConfig = futureConfigList.First(x => x.MarketOpenTime.Hours >= 5 && x.MarketOpenTime.Hours < 14);
            }
            else if (now.Hour >= 14 && now.Hour < 21)
            {
                futureConfig = futureConfigList.First(x => x.MarketOpenTime.Hours >= 14 && x.MarketOpenTime.Hours < 21);
            }
            else
            {
                futureConfig = futureConfigList.First(x => x.MarketOpenTime.Hours >= 21);
                futureConfig.MarketCloseTime = futureConfig.MarketCloseTime.Add(TimeSpan.FromHours(24));
                // 取得美東時間（Eastern Time, ET），適用於紐約、華盛頓等
                TimeZoneInfo easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                // 取得當前美東時間
                DateTime currentEasternTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternTimeZone);
                // 判斷是否為夏令時間
                bool isDaylightSaving = easternTimeZone.IsDaylightSavingTime(currentEasternTime);
                if (!isDaylightSaving)
                {
                    futureConfig.MarketOpenTime = futureConfig.MarketOpenTime.Add(TimeSpan.FromHours(1));
                }
            }
            return futureConfig;
        }
        private string GetSettlementMonth()
        {
            DateTime now = _dateTimeService.GetTaiwanTime();
            DateTime thirdWednesday = GetThirdWednesday(now.Year, now.Month);
            string settlementMonth = "";
            if (now.Day <= thirdWednesday.Day ||
               (now.Day == thirdWednesday.Day + 1 && _targetFutureConfig.Period == EPeriod.Evening))
            {
                settlementMonth = now.ToString("yyyyMM");
            }
            else
            {
                settlementMonth = now.AddMonths(1).ToString("yyyyMM");
            }
            return settlementMonth;
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
