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
        private BlockingQueue<FutureOrder> _futureOrderMessageQueue;
        private int _first15MinuteHigh = 0;
        private int _first15MinuteLow = 0;
        private int _longProfitPoint = 0;
        private int _longStopLossPoint = 0;
        private int _shortProfitPoint = 0;
        private int _shortStopLossPoint = 0;
        private string _orderNo = "";
        private bool _hasLongContract = false;
        private bool _hasShortContract = false;
        private readonly TimeSpan _afterMarketOpen15Minute;
        private readonly TimeSpan _beforeMarketClose10Minute;
        private readonly ILogger _logger;
        private readonly string _futureAccount;
        private readonly string _futurePassword;
        private readonly FutureConfig _targetFutureConfig;
        private readonly int _maxOrderQuantity;
        private readonly int _profitPoint;
        private readonly int _stopLossPoint;
        private readonly IYuantaService _yuantaService;
        private readonly string _targetCommodityId;
        private readonly string _settlementMonth;
        private readonly string _tradeDate;
        private readonly Dictionary<string, string> _codeCommodityIdDict = new Dictionary<string, string>
        {
            { "TXF1", "FITX" },
            { "MXF1", "FIMTX" },
            { "MXF8", "FIMTX" },
            { "TMF0", "FITM" },
            { "TMF8", "FITM" }
        };
        public FutureTraderService(IConfiguration config, ILogger logger, IDateTimeService dateTimeService, IYuantaService yuantaService)
        {
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
            string environment = config.GetValue<string>("Environment").ToUpper();
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _logger = logger;
            _futureAccount = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("FutureAccount", EnvironmentVariableTarget.Machine) : "S98875005091";
            _futurePassword = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockPassword", EnvironmentVariableTarget.Machine) : "1234";
            DateTime now = dateTimeService.GetTaiwanTime();
            DateTime thirdWednesday = GetThirdWednesday(now.Year, now.Month);
            _settlementMonth = now.Day > thirdWednesday.Day ? now.AddMonths(1).ToString("yyyyMM") : now.ToString("yyyyMM");
            _tradeDate = now.ToString("yyyy/MM/dd");
            List<FutureConfig> futureConfigList = config.GetSection("TargetFuture").Get<List<FutureConfig>>();
            bool isDay = now.Hour < 14 ? true : false;
            _targetFutureConfig = now.Hour < 14 ? futureConfigList.First(x => x.IsDayMarket) : futureConfigList.First(x => !x.IsDayMarket);
            _maxOrderQuantity = config.GetValue<int>("MaxOrderQuantity");
            _profitPoint = config.GetValue<int>("ProfitPoint");
            _stopLossPoint = config.GetValue<int>("StopLossPoint");
            _first15MinuteHigh = config.GetValue<int>("First15MinuteHigh");
            _first15MinuteLow = config.GetValue<int>("First15MinuteLow");
            SetExitPoint();
            _afterMarketOpen15Minute = _targetFutureConfig.MarketOpenTime.Add(TimeSpan.FromMinutes(15));
            _beforeMarketClose10Minute = _targetFutureConfig.MarketCloseTime.Subtract(TimeSpan.FromMinutes(10));
            _yuantaService = yuantaService;
            _futureOrderMessageQueue = new BlockingQueue<FutureOrder>(ProcessFutureOrder);
            if (!_codeCommodityIdDict.TryGetValue(_targetFutureConfig.FutureCode, out _targetCommodityId)) throw new Exception($"Can't find commodityId by code {_targetFutureConfig.FutureCode}");
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
                                //RealReportHandler(strResult);
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
            futureOrder.OpenOffsetKind = "2";                                               //新平倉碼, 0:新倉 1:平倉 2:自動                                          
            futureOrder.OrderCond = "";                                                     //委託條件, "":ROD 1:FOK 2:IOC
            futureOrder.DayTradeID = "Y";                                                   //當沖註記, Y:當沖  "":非當沖
            futureOrder.SellerNo = 0;                                                       //營業員代碼                                            
            futureOrder.OrderNo = "";                                                       //委託書編號           
            futureOrder.TradeDate = _tradeDate;                                             //交易日期                            
            futureOrder.BasketNo = "";                                                      //BasketNo
            futureOrder.Session = "";                                                       //通路種類, 1:預約 "":盤中單
            #region 組合單應填欄位
            futureOrder.CommodityID2 = "";                                                  //商品名稱2
            futureOrder.CallPut2 = "";                                                      //買賣權2
            futureOrder.SettlementMonth2 = 0;                                               //商品年月2
            futureOrder.StrikePrice2 = 0;                                                   //屐約價2                 
            futureOrder.OrderQty2 = 0;                                                      //委託口數2
            futureOrder.BuySell2 = "";                                                      //買賣別2
            #endregion
            return futureOrder;
        }
        private void FutureOrder(TimeSpan tickTime, int tickPrice)
        {
            if (tickTime == TimeSpan.Zero || tickPrice == 0) return;
            if (_first15MinuteHigh == 0 || _first15MinuteLow == 0 ||
                _longProfitPoint == 0 || _longStopLossPoint == 0 ||
                _shortProfitPoint == 0 || _shortStopLossPoint == 0) return;
            if (_hasLongContract)
            {
                if (tickPrice >= _longProfitPoint || tickPrice <= _longStopLossPoint || tickTime >= _beforeMarketClose10Minute)
                {
                    FutureOrder futureOrder = SetDefaultFutureOrder();
                    futureOrder.Price = tickPrice * 10000;                               //委託價格
                    futureOrder.BuySell1 = EBuySellType.S.ToString();                   //買賣別, "B":買 "S":賣
                    futureOrder.OrderType = ((int)EFutureOrderType.市價).ToString();     //委託方式, 1:市價 2:限價 3:範圍市價
                    _futureOrderMessageQueue.Add(futureOrder);
                }
            }
            else if (_hasShortContract)
            {
                if (tickPrice <= _shortProfitPoint && tickPrice >= _shortStopLossPoint || tickTime >= _beforeMarketClose10Minute)
                {
                    FutureOrder futureOrder = SetDefaultFutureOrder();
                    futureOrder.Price = tickPrice * 10000;
                    futureOrder.BuySell1 = EBuySellType.B.ToString();
                    futureOrder.OrderType = ((int)EFutureOrderType.市價).ToString();
                    _futureOrderMessageQueue.Add(futureOrder);
                }
            }
            else if (string.IsNullOrEmpty(_orderNo) && tickTime < _targetFutureConfig.LastEntryTime)
            {
                if (tickPrice > _first15MinuteHigh)
                {
                    FutureOrder futureOrder = SetDefaultFutureOrder();
                    futureOrder.Price = _first15MinuteHigh * 10000;                     //委託價格
                    futureOrder.BuySell1 = EBuySellType.B.ToString();                 //買賣別, "B":買 "S":賣
                    futureOrder.OrderType = ((int)EFutureOrderType.限價).ToString();   //委託方式, 1:市價 2:限價 3:範圍市價
                    _futureOrderMessageQueue.Add(futureOrder);
                }
                else if (tickPrice < _first15MinuteLow)
                {
                    FutureOrder futureOrder = SetDefaultFutureOrder();
                    futureOrder.Price = _first15MinuteLow * 10000;                      //委託價格
                    futureOrder.BuySell1 = EBuySellType.S.ToString();                 //買賣別, "B":買 "S":賣
                    futureOrder.OrderType = ((int)EFutureOrderType.限價).ToString();   //委託方式, 1:市價 2:限價 3:範圍市價
                    _futureOrderMessageQueue.Add(futureOrder);
                }
            }
            else if (!string.IsNullOrEmpty(_orderNo) && tickTime >= _targetFutureConfig.LastEntryTime)
            {
                FutureOrder futureOrder = SetDefaultFutureOrder();
                futureOrder.Price = 0;
                futureOrder.BuySell1 = "";
                futureOrder.OrderType = "";
                futureOrder.FunctionCode = 4;                                         //功能別, 0:委託單 4:取消 5:改量 7:改價
                futureOrder.OrderNo = _orderNo;                                                       //委託書編號    
                _futureOrderMessageQueue.Add(futureOrder);
            }
        }
        private void ProcessFutureOrder(FutureOrder futureOrder)
        {
            if (!string.IsNullOrEmpty(_orderNo) && futureOrder.FunctionCode != 4) return;
            if (_hasLongContract && futureOrder.BuySell1 != EBuySellType.S.ToString()) return;
            if (_hasShortContract && futureOrder.BuySell1 != EBuySellType.B.ToString()) return;
            bool bResult = objYuantaOneAPI.SendFutureOrder(_futureAccount, new List<FutureOrder>() { futureOrder });
            if (bResult)
            {
                _orderNo = "ordered";
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
