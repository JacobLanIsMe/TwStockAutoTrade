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
        enumLangType enumLng = enumLangType.NORMAL;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private ConcurrentBag<int> _first5MinuteTickBag = new ConcurrentBag<int>();
        private BlockingCollection<string> _messageQueue = new BlockingCollection<string>(new ConcurrentQueue<string>());
        private int contractQuantity = 0;
        private int _first5MinuteHigh = 0;
        private int _first5MinuteLow = 0;
        private TimeSpan _marketOpenTime;
        private TimeSpan _afterMarketOpen5Minute;
        private readonly ILogger _logger;
        private readonly string _futureAccount;
        private readonly string _futurePassword;
        private readonly string _targetFutureCode;
        private readonly int _maxOrderQuantity;
        private Dictionary<string, string> _codeCommodityIdDict = new Dictionary<string, string>
        {
            { "TXF1", "FITX" },
            { "MXF1", "FIMTX" },
            { "TMF0", "FITM" }
        };
        public FutureTraderService(IConfiguration config, ILogger logger)
        {
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
            string environment = config.GetValue<string>("Environment").ToUpper();
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _logger = logger;
            _futureAccount = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("FutureAccount") : "S98875005091";
            _futurePassword = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("FuturePassword") : "1234";
            _targetFutureCode = config.GetValue<string>("TargetFutureCode");
            _maxOrderQuantity = config.GetValue<int>("MaxOrderQuantity");
            if (_targetFutureCode == "TMF0" || _targetFutureCode == "MXF1" || _targetFutureCode == "TXF1")
            {
                _marketOpenTime = new TimeSpan(8, 45, 0);
            }
            else if (_targetFutureCode == "MXF8")
            {
                _marketOpenTime = new TimeSpan(15, 0, 0);
            }
            else
            {
                throw new Exception("Target future code is not supported.");
            }
            _afterMarketOpen5Minute = _marketOpenTime.Add(TimeSpan.FromMinutes(5));
        }
        public async Task Trade()
        {
            try
            {
                Task.Run(() =>
                {
                    foreach (var message in _messageQueue.GetConsumingEnumerable())
                    {
                        ProcessMessage(message);
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
            if (tickTime < _afterMarketOpen5Minute && tickTime >= _marketOpenTime)
            {
                _first5MinuteTickBag.Add(tickPrice);
            }
            if (_first5MinuteHigh == 0 && _first5MinuteLow == 0 && tickTime >= _afterMarketOpen5Minute)
            {
                _first5MinuteHigh = _first5MinuteTickBag.Max();
                _first5MinuteLow = _first5MinuteTickBag.Min();
            }
        }
        private void FutureOrder(int tickPrice)
        {
            List<FutureOrder> lstFutureOrder = new List<FutureOrder>();
            FutureOrder futureOrder = new FutureOrder();

            futureOrder.Identify = 001;                                //識別碼
            futureOrder.Account = _futureAccount;                                                    //期貨帳號
            futureOrder.FunctionCode = 0;                           //功能別, 0:委託單 4:取消 5:改量 7:改價                     
            futureOrder.CommodityID1 = _codeCommodityIdDict[_targetFutureCode];                                            //商品名稱1
            futureOrder.CallPut1 = "";                                                    //買賣權1
            futureOrder.SettlementMonth1 = Convert.ToInt32(this.txtSettlementMonth1.Text.Trim());                   //商品年月1
            futureOrder.StrikePrice1 = Convert.ToInt32(Convert.ToDecimal(this.txtStrikePrice1.Text.Trim()) * 1000); //屐約價1
            futureOrder.Price = Convert.ToInt32(Convert.ToDecimal(this.txtOrderPrice1.Text.Trim()) * 10000);        //委託價格
            futureOrder.OrderQty1 = Convert.ToInt16(this.txtOrderQty1.Text.Trim());                                 //委託口數1
            futureOrder.BuySell1 = this.txtBuySell1.Text.Trim();                                                    //買賣別
            futureOrder.CommodityID2 = this.txtCommodityID2.Text.Trim();                                            //商品名稱2
            futureOrder.CallPut2 = this.txtCallPut2.Text.Trim();                                                    //買賣權2
            futureOrder.SettlementMonth2 = Convert.ToInt32(this.txtSettlementMonth2.Text.Trim());                   //商品年月2
            futureOrder.StrikePrice2 = Convert.ToInt32(Convert.ToDecimal(this.txtStrikePrice2.Text.Trim()) * 1000); //屐約價2                 
            futureOrder.OrderQty2 = Convert.ToInt16(this.txtOrderQty2.Text.Trim());                                 //委託口數2
            futureOrder.BuySell2 = this.txtBuySell2.Text.Trim();                                                   //買賣別2
            futureOrder.OpenOffsetKind = this.txtOpenOffsetKind.Text.Trim();                                        //新平倉碼                                              
            futureOrder.DayTradeID = this.txtDayTradeID.Text;                                                       //當沖註記
            futureOrder.OrderType = this.txtFutOrderType.Text.Trim();                                               //委託方式    
            futureOrder.OrderCond = this.txtOrderCond.Text;                                                         //委託條件                                           
            futureOrder.SellerNo = Convert.ToInt16(this.txtFutSellerNo.Text.Trim());                                //營業員代碼                                            
            futureOrder.OrderNo = this.txtFutOrderNo.Text.Trim();                                                  //委託書編號           
            futureOrder.TradeDate = this.txtFutTradeDate.Text;                                                      //交易日期                            
            futureOrder.BasketNo = this.txtFutBasketNo.Text.Trim();                                                 //BasketNo
            futureOrder.Session = this.txtSession.Text.Trim();                                                     //通路種類                                    

            lstFutureOrder.Add(futureOrder);

            bool bResult = objYuantaOneAPI.SendFutureOrder(this.cboAccountList.SelectedItem != null ? this.cboAccountList.SelectedItem.ToString().Trim() : "", lstFutureOrder);
            if (tickPrice > _first5MinuteHigh && tickPrice < _first5MinuteHigh + 3)
            {

            }
            else if (tickPrice < _first5MinuteLow && tickPrice > _first5MinuteLow - 3)
            {

            }
            else
            {
                return;
            }
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
    }
}
