using Core.Enum;
using Core.HttpClientFactory;
using Core.Model;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        private int _high = 0;
        private int _low = 0;
        private int _volume = 0;
        private int _prevVolume = 0;
        private FutureTrade _trade = new FutureTrade();
        private bool _hasFutureOrder = false;
        private bool _isTradingStarted = false;
        private readonly TimeSpan _beforeMarketClose5Minute;
        private readonly TimeSpan _lastEntryTime;
        private readonly ILogger _logger;
        private readonly string _futureAccount;
        private readonly string _futurePassword;
        private readonly FutureConfig _targetFutureConfig;
        private readonly int _maxOrderQuantity;
        private readonly IYuantaService _yuantaService;
        private readonly IDateTimeService _dateTimeService;
        private readonly string _settlementMonth;
        private int _settlementPrice = 0;
        private int _stopLossPoint = 0;
        private int _longLimitPoint = 0;
        private int _shortLimitPoint = 0;
        private readonly IDiscordService _discordService;

        public FutureTraderService(IConfiguration config, ILogger logger, IDateTimeService dateTimeService, IYuantaService yuantaService, IDiscordService discordService)
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
            DateTime now = _dateTimeService.GetTaiwanTime();
            _settlementMonth = GetSettlementMonth(now);
            _targetFutureConfig = SetFutureConfig(config);
            _beforeMarketClose5Minute = _targetFutureConfig.MarketCloseTime.Subtract(TimeSpan.FromMinutes(5));
            _lastEntryTime = _targetFutureConfig.MarketCloseTime.Subtract(TimeSpan.FromHours(1));
            _discordService = discordService;
        }
        public async Task Trade()
        {
            try
            {
                _cts.CancelAfter(TimeSpan.FromHours(8));
                await SetPrevVolume();
                await PrintConfig();
                //await SetLimitPoint();
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
                                string tickResult = _yuantaService.FunRealStocktick_Out((byte[])objValue);
                                TickHandler(tickResult, out TimeSpan tickTime, out int tickPrice);
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
        private void TickHandler(string strResult, out TimeSpan tickTime, out int tickPrice)
        {
            tickPrice = 0;
            tickTime = TimeSpan.Zero;
            _logger.Information($"TickHandler: {strResult}");
            if (string.IsNullOrEmpty(strResult)) return;
            string[] tickInfo = strResult.Split(',');
            if (!TimeSpan.TryParse(tickInfo[3], out tickTime))
            {
                _logger.Error("Tick time failed in TickHandler");
                return;
            }
            if (tickTime < _targetFutureConfig.MarketOpenTime) return;

            if (!int.TryParse(tickInfo[6], out tickPrice))
            {
                _logger.Error("Tick price failed in TickHandler");
                return;
            }
            tickPrice = tickPrice / 1000;
            _logger.Information($"Tick time: {tickTime}, Tick price: {tickPrice}");
            if (tickTime < _targetFutureConfig.TimeThreshold)
            {
                if (_high == 0 && _low == 0)
                {
                    _high = tickPrice;
                    _low = tickPrice;
                }
                else
                {
                    if (tickPrice > _high)
                    {
                        _high = tickPrice;
                    }
                    if (tickPrice < _low)
                    {
                        _low = tickPrice;
                    }
                }
                if (int.TryParse(tickInfo[7], out int tickVolume))
                {
                    _volume += tickVolume;
                    _logger.Information($"Tick time: {tickTime}, Tick volume: {tickVolume}, Total volume: {_volume}");
                }
            }
            else
            {
                if (_isTradingStarted) return;
                if (_volume > _prevVolume * 0.3 && _high != 0 && _low != 0 && _high - _low > 100 && _low <= (_settlementPrice + (_settlementPrice * 0.09)))
                {
                    _stopLossPoint = (int)((double)_low + _settlementPrice * 0.004);
                    _logger.Information($"開盤後成交量達前一個交易日成交量的30%，高低點差大於100點，達進場條件");
                    _logger.Information($"停損點設在: {_stopLossPoint}");
                    _isTradingStarted = true;
                }
                else
                {
                    _logger.Information("開盤後成交量小於前一個交易日成交量的30%或是高低點差小於100點，未達進場條件");
                    _cts.Cancel();
                }
            }
            _logger.Information($"Tick time: {tickTime}, High: {_high}, Log: {_low}");
        }
        private void FutureOrder(TimeSpan tickTime, int tickPrice)
        {
            if (!_isTradingStarted || tickTime == TimeSpan.Zero || tickPrice == 0 || _hasFutureOrder) return;
            if (_trade.OpenOffsetKind == EOpenOffsetKind.新倉)
            {
                if (tickPrice > _stopLossPoint || tickTime > _beforeMarketClose5Minute)
                {
                    ProcessFutureOrder(SetFutureOrder(EBuySellType.B));
                }
            }
            else
            {
                if (tickPrice < _low && tickTime < _lastEntryTime)
                {
                    ProcessFutureOrder(SetFutureOrder(EBuySellType.S));
                }
            }
        }
        private void ProcessFutureOrder(FutureOrder futureOrder)
        {
            bool bResult = objYuantaOneAPI.SendFutureOrder(_futureAccount, new List<FutureOrder>() { futureOrder });
            if (bResult)
            {
                _hasFutureOrder = true;
                _logger.Information($"SendFutureOrder success. Buy or Sell: {futureOrder.BuySell1}");
            }
            else
            {
                _logger.Error($"SendFutureOrder error. Buy or Sell: {futureOrder.BuySell1}");
            }
        }
        private FutureOrder SetFutureOrder(EBuySellType eBuySellType)
        {
            FutureOrder futureOrder = new FutureOrder();
            futureOrder.Identify = 001;                                                      //識別碼
            futureOrder.Account = _futureAccount;                                            //期貨帳號
            futureOrder.FunctionCode = 0;                                                    //功能別, 0:委託單 4:取消 5:改量 7:改價                     
            futureOrder.CommodityID1 = _targetFutureConfig.CommodityId;                      //商品名稱1
            futureOrder.CallPut1 = "";                                                       //買賣權1
            futureOrder.SettlementMonth1 = Convert.ToInt32(_settlementMonth);                //商品年月1
            futureOrder.StrikePrice1 = 0;                                                    //屐約價1
            futureOrder.Price = 0;                                                          //委託價格
            futureOrder.OrderQty1 = Convert.ToInt16(_maxOrderQuantity);                      //委託口數1
            futureOrder.OrderType = ((int)EFutureOrderType.市價).ToString();     //委託方式, 1:市價 2:限價 3:範圍市價
            futureOrder.OrderCond = "1";                                                      //委託條件, "":ROD 1:FOK 2:IOC
            futureOrder.OpenOffsetKind = "2";                                                //新平倉碼, 0:新倉 1:平倉 2:自動       
            futureOrder.BuySell1 = eBuySellType.ToString();                                   //買賣別1, "B":買 "S":賣
            futureOrder.DayTradeID = "";                                                    //當沖註記, Y:當沖  "":非當沖
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
                _hasFutureOrder = false;
            }
        }
        private void RealReportHandler(string strResult)
        {
            string[] reportArray = strResult.Split(',');
            if (!int.TryParse(reportArray[1].Split(':')[1], out int reportType))    // 回報類別
            {
                _logger.Error("Report type error");
            }
            if (reportType == 3)
            {
                if (!System.Enum.TryParse<EBuySellType>(reportArray[9], out EBuySellType buySell))  // 買賣別
                {
                    _logger.Error("BuySellType error");
                }
                if (!int.TryParse(reportArray[10], out int point))
                {
                    _logger.Error("Point error");
                }
                if (!int.TryParse(reportArray[13], out int lot))    // 委託口數
                {
                    _logger.Error("Lot error");
                }
                if (!int.TryParse(reportArray[14], out int openOffsetKind))     // 新平倉碼, 0:新倉 1:平倉
                {
                    _logger.Error("OpenOffsetKind error");
                }
                _trade.BuySell = buySell;
                if (openOffsetKind == 0)
                {
                    _trade.OpenOffsetKind = EOpenOffsetKind.新倉;
                    if (_trade.Point == 0)
                    {
                        _trade.Point = point;
                    }
                    else
                    {
                        if (buySell == EBuySellType.B)
                        {
                            _trade.Point = Math.Max(_trade.Point, point);
                        }
                        else
                        {
                            _trade.Point = Math.Min(_trade.Point, point);
                        }
                    }
                    _trade.PurchasedLot = _trade.PurchasedLot + lot;
                    if (_trade.PurchasedLot == _maxOrderQuantity)
                    {
                        _hasFutureOrder = false;
                    }
                }
                else
                {
                    _trade.OpenOffsetKind = EOpenOffsetKind.平倉;
                    _trade.Point = 0;
                    _trade.PurchasedLot = _trade.PurchasedLot - lot;
                    if (_trade.PurchasedLot == 0)
                    {
                        _logger.Information($"平倉成功");
                        _cts.Cancel();
                        _hasFutureOrder = false;
                    }
                }
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
        private FutureConfig SetFutureConfig(IConfiguration config)
        {
            FutureConfig futureConfig = config.GetSection("TargetFuture").Get<FutureConfig>();
            Dictionary<string, string> monthMap = new Dictionary<string, string>
            {
                { "01", "A" },
                { "02", "B" },
                { "03", "C" },
                { "04", "D" },
                { "05", "E" },
                { "06", "F" },
                { "07", "G" },
                { "08", "H" },
                { "09", "I" },
                { "10", "J" },
                { "11", "K" },
                { "12", "L" }
            };
            if (!monthMap.TryGetValue(_settlementMonth.Substring(4), out string code)) throw new Exception("Can not find future code");
            futureConfig.FutureCode = $"{futureConfig.FutureCode}{code}5";
            return futureConfig;
        }
        private string GetSettlementMonth(DateTime dateTime)
        {
            DateTime thirdWednesday = GetThirdWednesday(dateTime.Year, dateTime.Month);
            string settlementMonth = "";
            if (dateTime.Day < thirdWednesday.Day)
            {
                settlementMonth = dateTime.ToString("yyyyMM");
            }
            else
            {
                settlementMonth = dateTime.AddMonths(1).ToString("yyyyMM");
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
        private async Task SetPrevVolume()
        {
            SimpleHttpClientFactory httpClientFactory = new SimpleHttpClientFactory();
            HttpClient httpClient = httpClientFactory.CreateClient();
            HttpResponseMessage response = await httpClient.GetAsync("https://openapi.taifex.com.tw/v1/DailyMarketReportFut");
            string responseBody = await response.Content.ReadAsStringAsync();
            List<TaifexDailyMarketReport> futureReportList = JsonConvert.DeserializeObject<List<TaifexDailyMarketReport>>(responseBody);
            var prevFutureReport = futureReportList.Where(x => x.Contract == _targetFutureConfig.FutureCode.Substring(0, 3) && x.TradingSession == "一般" && x.ContractMonth == _settlementMonth).FirstOrDefault();
            if (prevFutureReport == null || 
                !int.TryParse(prevFutureReport.Volume, out _prevVolume) || 
                _prevVolume == 0 || 
                !int.TryParse(prevFutureReport.SettlementPrice, out _settlementPrice) || 
                _settlementPrice == 0)
            {
                await _discordService.SendMessage("取得前一個交易日的交易資訊出現錯誤");
                _logger.Error("取得前一個交易日的交易資訊出現錯誤");
                _cts.Cancel();
            }
        }
        private async Task SetLimitPoint()
        {
            SimpleHttpClientFactory httpClientFactory = new SimpleHttpClientFactory();
            HttpClient httpClient = httpClientFactory.CreateClient();
            HttpResponseMessage response = await httpClient.GetAsync("https://tw.screener.finance.yahoo.net/future/q?type=tick&perd=1m&mkt=01&sym=WTX%26&callback=jQuery111304508811793071039_1744181429926&_=1744181429927");
            string responseBody = await response.Content.ReadAsStringAsync();
            string key = "\"129\":";
            int startIndex = responseBody.IndexOf(key);
            if (startIndex != -1)
            {
                startIndex += key.Length;
                int endIndex = responseBody.IndexOf(",", startIndex);
                string settlementPriceStr = responseBody.Substring(startIndex, endIndex - startIndex).Trim();
                if (!decimal.TryParse(settlementPriceStr, out decimal value)) throw new Exception("Settlement price parse error");
                int settlementPrice = (int)value;
                _longLimitPoint = (int)(settlementPrice - ((double)settlementPrice * 0.09));
                _shortLimitPoint = (int)(settlementPrice + ((double)settlementPrice * 0.09));
                _logger.Information($"前一個交易日結算價: {settlementPrice}");
                _logger.Information($"多單限價: {_longLimitPoint} 以上才可做多");
                _logger.Information($"空單限價: {_shortLimitPoint} 以下才可做空");
            }
            else
            {
                throw new Exception("Can not find sellement price from Yahoo url");
            }
        }
        private async Task PrintConfig()
        {
            _logger.Information($"開盤時間: {_targetFutureConfig.MarketOpenTime}");
            _logger.Information($"收盤時間: {_targetFutureConfig.MarketCloseTime}");
            _logger.Information($"收盤前五分鐘時間: {_beforeMarketClose5Minute}");
            _logger.Information($"最後進場時間: {_lastEntryTime}");
            _logger.Information($"商品代碼: {_targetFutureConfig.FutureCode}");
            _logger.Information($"商品名稱: {_targetFutureConfig.CommodityId}");
            _logger.Information($"商品年月: {_settlementMonth}");
            _logger.Information($"前一個交易日的結算價格: {_settlementPrice}");
            _logger.Information($"前一天的成交量: {_prevVolume}");
            string message = $"開盤時間: {_targetFutureConfig.MarketOpenTime}\n";
            message += $"收盤時間: {_targetFutureConfig.MarketCloseTime}\n";
            message += $"收盤前五分鐘時間: {_beforeMarketClose5Minute}\n";
            message += $"最後進場時間: {_lastEntryTime}\n";
            message += $"商品代碼: {_targetFutureConfig.FutureCode}\n";
            message += $"商品名稱: {_targetFutureConfig.CommodityId}\n";
            message += $"商品年月: {_settlementMonth}\n";
            message += $"前一個交易日的結算價格: {_settlementPrice}\n";
            message += $"前一個交易日的成交量: {_prevVolume}\n";
            await _discordService.SendMessage(message);
        }
    }
}
