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
        private readonly enumLangType enumLng = enumLangType.NORMAL;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private ConcurrentBag<int> _first5MinuteTickBag = new ConcurrentBag<int>();
        private BlockingCollection<FutureOrder> _futureOrderMessageQueue = new BlockingCollection<FutureOrder>(new ConcurrentQueue<FutureOrder>());
        private int contractQuantity = 0;
        private int _first5MinuteHigh = 0;
        private int _first5MinuteLow = 0;
        private int _longProfitPoint = 0;
        private int _longStopLossPoint = 0;
        private int _shortProfitPoint = 0;
        private int _shortStopLossPoint = 0;
        private List<FutureOrder> _lstFutureOrder = new List<FutureOrder>();
        private FutureOrder _futureOrder = new FutureOrder();
        private bool hasLongOrder = false;
        private bool hasLongContract = false;
        private bool hasShortOrder = false;
        private bool hasShortContract = false;
        private readonly TimeSpan _marketOpenTime = new TimeSpan(8, 45, 0);
        private readonly TimeSpan _afterMarketOpen5Minute;
        private readonly ILogger _logger;
        private readonly string _futureAccount;
        private readonly string _futurePassword;
        private readonly string _targetFutureCode;
        private readonly int _maxOrderQuantity;
        private readonly int _profitPoint;
        private readonly int _stopLossPoint;
        private readonly IDateTimeService _dateTimeService;
        private readonly Dictionary<string, string> _codeCommodityIdDict = new Dictionary<string, string>
        {
            { "TXF1", "FITX" },
            { "MXF1", "FIMTX" },
            { "TMF0", "FITM" }
        };
        public FutureTraderService(IConfiguration config, ILogger logger, IDateTimeService dateTimeService)
        {
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
            string environment = config.GetValue<string>("Environment").ToUpper();
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _logger = logger;
            _futureAccount = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("FutureAccount") : "S98875005091";
            _futurePassword = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("FuturePassword") : "1234";
            _targetFutureCode = config.GetValue<string>("TargetFutureCode");
            _maxOrderQuantity = config.GetValue<int>("MaxOrderQuantity");
            _profitPoint = config.GetValue<int>("ProfitPoint");
            _stopLossPoint = config.GetValue<int>("StopLossPoint");
            _afterMarketOpen5Minute = _marketOpenTime.Add(TimeSpan.FromMinutes(5));
            _dateTimeService = dateTimeService;
            SetDefaultFutureOrder();
            _lstFutureOrder.Add(_futureOrder);
        }
        public async Task Trade()
        {
            try
            {
                Task.Run(() =>
                {
                    foreach (FutureOrder order in _futureOrderMessageQueue.GetConsumingEnumerable())
                    {
                        ProcessFutureOrder(order);
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
                        SystemResponseHandler(strResult);
                        break;
                    case 1: //代表為RQ/RP 所回應的
                        switch (strIndex)
                        {
                            case "Login":       //一般/子帳登入
                                strResult = FunAPILogin_Out((byte[])objValue);
                                break;
                            default:           //不在表列中的直接呈現訊息
                                strResult = $"{strIndex},{objValue}";
                                break;
                        }
                        break;
                    case 2: //訂閱所回應
                        switch (strIndex)
                        {
                            case "210.10.40.10":    //訂閱個股分時明細
                                strResult = FunRealStocktick_Out((byte[])objValue);
                                TickHandler(strResult, out int tickPrice);
                                FutureOrder(tickPrice);
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
        private void SystemResponseHandler(string strResult)
        {
            if (strResult == "交易主機Is Connected!!")
            {
                objYuantaOneAPI.Login(_futureAccount, _futurePassword);
            }
            else if (strResult == "台股報價/國內期貨報價/國外期貨報價Is Connected!!")
            {
                SubscribeFutureTick();
            }
            else
            {
                _cts.Cancel();
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
        private void SetDefaultFutureOrder()
        {
            if (!_codeCommodityIdDict.TryGetValue(_targetFutureCode, out string commodityId)) throw new Exception($"Can't find commodityId by code {_targetFutureCode}");
            DateTime now = _dateTimeService.GetTaiwanTime();
            DateTime thirdWednesday = GetThirdWednesday(now.Year, now.Month);
            string settlementMonth = now.Day > thirdWednesday.Day ? now.AddMonths(1).ToString("yyyyMM") : now.ToString("yyyyMM");
            _futureOrder.Identify = 001;                                                     //識別碼
            _futureOrder.Account = _futureAccount;                                           //期貨帳號
            _futureOrder.FunctionCode = 0;                                                   //功能別, 0:委託單 4:取消 5:改量 7:改價                     
            _futureOrder.CommodityID1 = commodityId;                                         //商品名稱1
            _futureOrder.CallPut1 = "";                                                      //買賣權1
            _futureOrder.SettlementMonth1 = Convert.ToInt32(settlementMonth);                //商品年月1
            _futureOrder.StrikePrice1 = Convert.ToInt32(Convert.ToDecimal(this.txtStrikePrice1.Text.Trim()) * 1000); //屐約價1
            _futureOrder.OrderQty1 = Convert.ToInt16(_maxOrderQuantity);                     //委託口數1
            #region 組合單應填欄位
            _futureOrder.CommodityID2 = "";                                                  //商品名稱2
            _futureOrder.CallPut2 = "";                                                      //買賣權2
            _futureOrder.SettlementMonth2 = 0;                                               //商品年月2
            _futureOrder.StrikePrice2 = 0;                                                   //屐約價2                 
            _futureOrder.OrderQty2 = 0;                                                      //委託口數2
            _futureOrder.BuySell2 = "";                                                      //買賣別2
            #endregion
            _futureOrder.OpenOffsetKind = 2.ToString();                                      //新平倉碼, 0:新倉 1:平倉 2:自動                                          
            _futureOrder.DayTradeID = "Y";                                                   //當沖註記, Y:當沖  "":非當沖
            _futureOrder.SellerNo = Convert.ToInt16(this.txtFutSellerNo.Text.Trim());        //營業員代碼                                            
            _futureOrder.OrderNo = this.txtFutOrderNo.Text.Trim();                           //委託書編號           
            _futureOrder.TradeDate = now.ToString("yyyy/MM/dd");                             //交易日期                            
            _futureOrder.BasketNo = this.txtFutBasketNo.Text.Trim();                         //BasketNo
            _futureOrder.Session = "";                                                       //通路種類, 1:預約 "":盤中單                             
        }
        private void FutureOrder(int tickPrice)
        {
            if (!hasLongOrder && !hasLongContract && tickPrice > _first5MinuteHigh && tickPrice < _first5MinuteHigh + 5)
            {
                FutureOrder futureOrder = new FutureOrder();
                futureOrder.Price = _first5MinuteHigh * 1000;                    //委託價格
                futureOrder.BuySell1 = EBuySellType.B.ToString();                 //買賣別, "B":買 "S":賣
                futureOrder.OrderType = ((int)EFutureOrderType.限價).ToString();   //委託方式, 1:市價 2:限價 3:範圍市價
                futureOrder.OrderCond = "";                                       //委託條件, "":ROD 1:FOK 2:IOC
                _futureOrderMessageQueue.Add(futureOrder);
            }
            else if (hasLongContract && (tickPrice <= _longStopLossPoint || tickPrice >= _longProfitPoint))
            {
                FutureOrder futureOrder = new FutureOrder();
                futureOrder.Price = tickPrice * 1000;
                futureOrder.BuySell1 = EBuySellType.S.ToString();
                futureOrder.OrderType = ((int)EFutureOrderType.市價).ToString();
                futureOrder.OrderCond = "";
                _futureOrderMessageQueue.Add(futureOrder);
            }
            else if (!hasShortOrder && !hasShortContract && tickPrice < _first5MinuteLow && tickPrice > _first5MinuteLow - 5)
            {
                FutureOrder futureOrder = new FutureOrder();
                futureOrder.Price = _first5MinuteLow * 1000;                     //委託價格
                futureOrder.BuySell1 = EBuySellType.S.ToString();                 //買賣別, "B":買 "S":賣
                futureOrder.OrderType = ((int)EFutureOrderType.限價).ToString();   //委託方式, 1:市價 2:限價 3:範圍市價
                futureOrder.OrderCond = "";                                       //委託條件, "":ROD 1:FOK 2:IOC
                _futureOrderMessageQueue.Add(futureOrder);
            }
            else if (hasShortContract && (tickPrice >= _shortStopLossPoint || tickPrice <= _shortProfitPoint))
            {
                FutureOrder futureOrder = new FutureOrder();
                futureOrder.Price = tickPrice * 1000;
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
        private void ProcessFutureOrder(FutureOrder order)
        {
            if (!hasLongOrder && !hasLongContract && order.BuySell1 == EBuySellType.B.ToString() && order.OrderType == ((int)EFutureOrderType.限價).ToString())
            {

            }

            _futureOrder.Price = order.Price;
            _futureOrder.BuySell1 = order.BuySell1;
            _futureOrder.OrderType = order.OrderType;
            _futureOrder.OrderCond = order.OrderCond;
            bool bResult = objYuantaOneAPI.SendFutureOrder(_futureAccount, _lstFutureOrder);
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
        private string FunAPILogin_Out(byte[] abyData)
        {
            string strResult = "";
            try
            {
                SgnAPILogin.ParentStruct_Out struParentOut = new SgnAPILogin.ParentStruct_Out();
                SgnAPILogin.ChildStruct_Out struChildOut = new SgnAPILogin.ChildStruct_Out();

                YuantaDataHelper dataGetter = new YuantaDataHelper(enumLng);
                dataGetter.OutMsgLoad(abyData);
                {
                    string strMsgCode = "";
                    string strMsgContent = "";
                    int intCount = 0;
                    strMsgCode = dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyMsgCode));
                    strMsgContent = dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyMsgContent));
                    intCount = (int)dataGetter.GetUInt();

                    strResult += FilterBreakChar(strMsgCode) + "," + FilterBreakChar(strMsgContent) + "\r\n";
                    if (strMsgCode == "0001" || strMsgCode == "00001")
                    {
                        strResult += "帳號筆數: " + intCount.ToString() + "\r\n";
                        for (int i = 0; i < intCount; i++)
                        {
                            string strAccount = "", strSubAccount = "", strID = "";
                            short shtSellNo = 0;
                            strAccount = dataGetter.GetStr(Marshal.SizeOf(struChildOut.abyAccount));
                            strSubAccount = dataGetter.GetStr(Marshal.SizeOf(struChildOut.abySubAccName));
                            strID = dataGetter.GetStr(Marshal.SizeOf(struChildOut.abyInvesotrID));
                            strResult += FilterBreakChar(strAccount) + ",";
                            strResult += FilterBreakChar(strSubAccount) + ",";
                            strResult += FilterBreakChar(strID) + ",";
                            shtSellNo = dataGetter.GetShort();
                            strResult += shtSellNo.ToString() + ",";
                            strResult += "\r\n";
                        }
                    }
                    else
                    {
                        throw new Exception("Login failed");
                    }
                }
            }
            catch (Exception ex)
            {
                strResult = ex.Message;
            }
            return strResult;
        }
        private string FilterBreakChar(string strFilterData)
        {
            Encoding enc = Encoding.GetEncoding("Big5");//提供Big5的編解碼
            byte[] tmp_bytearyData = enc.GetBytes(strFilterData);
            int intCharLen = tmp_bytearyData.Length;
            int indexCharData = intCharLen;
            for (int i = 0; i < intCharLen; i++)
            {
                if (Convert.ToChar(tmp_bytearyData.GetValue(i)) == 0)
                {
                    indexCharData = i;
                    break;
                }
            }
            return enc.GetString(tmp_bytearyData, 0, indexCharData);
        }
        /// <summary>
        /// 分時明細(即時訂閱結果)
        /// </summary>
        /// <param name="abyData"></param>
        /// <returns></returns>
        private string FunRealStocktick_Out(byte[] abyData)
        {
            string strResult = "";
            try
            {
                RR_WatclistAll.ParentStruct_Out struParentOut = new RR_WatclistAll.ParentStruct_Out();
                TYuantaTime yuantaTime;
                YuantaDataHelper dataGetter = new YuantaDataHelper(enumLng);
                dataGetter.OutMsgLoad(abyData);

                {
                    strResult += "分時明細訂閱結果: \r\n";
                    string strTemp = "";
                    byte byTemp = new byte();
                    int intTemp = 0;

                    strTemp = dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyKey));          //鍵值
                    byTemp = dataGetter.GetByte();                                              //市場代碼
                    strResult += byTemp.ToString() + ",";
                    strTemp = dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyStkCode));      //股票代碼
                    strResult += FilterBreakChar(strTemp) + ",";
                    intTemp = dataGetter.GetInt();                                              //序號
                    strResult += intTemp.ToString() + ",";
                    yuantaTime = dataGetter.GetTYuantaTime();                                   //時間
                    strTemp = String.Format(" {0}:{1}:{2}.{3}", yuantaTime.bytHour, yuantaTime.bytMin, yuantaTime.bytSec, yuantaTime.ushtMSec);
                    strResult += strTemp + ",";
                    intTemp = dataGetter.GetInt();                                              //買價
                    strResult += intTemp.ToString() + ",";
                    intTemp = dataGetter.GetInt();                                              //賣價
                    strResult += intTemp.ToString() + ",";
                    intTemp = dataGetter.GetInt();                                              //成交價
                    strResult += intTemp.ToString() + ",";
                    intTemp = dataGetter.GetInt();                                              //成交量
                    strResult += intTemp.ToString() + ",";
                    byTemp = dataGetter.GetByte();                                              //內外盤註記
                    strResult += byTemp.ToString() + ",";
                    byTemp = dataGetter.GetByte();                                              //明細類別
                    strResult += byTemp.ToString();

                    //----------
                    strResult += "\r\n";
                }
            }
            catch
            {
                strResult = "";
            }
            return strResult;
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
