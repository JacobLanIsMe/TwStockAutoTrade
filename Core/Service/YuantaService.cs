using Core.Model;
using Core.Service.Interface;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Service
{
    public class YuantaService : IYuantaService
    {
        enumLangType enumLng = enumLangType.NORMAL;
        private readonly ILogger _logger;
        public YuantaService(ILogger logger) 
        {
            _logger = logger;
        }
        public void SystemResponseHandler(string strResult, YuantaOneAPITrader objYuantaOneAPI, string account, string password, CancellationTokenSource cts, Action subscribeStockTick)
        {
            if (strResult == "交易主機Is Connected!!")
            {
                objYuantaOneAPI.Login(account, password);
            }
            else if (strResult == "台股報價/國內期貨報價/國外期貨報價Is Connected!!")
            {
                subscribeStockTick?.Invoke();
            }
            else if (strResult == "台股報價/國外期貨報價Is Connected!!")
            {
                subscribeStockTick?.Invoke();
            }
            else
            {
                cts.Cancel();
            }
        }
        public string FunAPILogin_Out(byte[] abyData)
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
        public string FunRealStocktick_Out(byte[] abyData)
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
        
        /// <summary>
        /// 五檔(即時訂閱結果)
        /// </summary>
        /// <param name="abyData"></param>
        /// <returns></returns>
        public string FunRealFivetick_Out(byte[] abyData)
        {
            string strResult = "";
            try
            {
                RR_WatclistAll.ParentStruct_Out struParentOut = new RR_WatclistAll.ParentStruct_Out();

                YuantaDataHelper dataGetter = new YuantaDataHelper(enumLng);
                dataGetter.OutMsgLoad(abyData);

                int intCheck = 1;
                if (intCheck == 1)
                {
                    strResult += "五檔訂閱結果: \r\n";
                    string strTemp = "";
                    byte byTemp = new byte();
                    int intTemp = 0;
                    uint uintTemp = 0;

                    strTemp = dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyKey));                  //鍵值
                    byTemp = dataGetter.GetByte();                                                      //市場代碼
                    strResult += byTemp.ToString() + ",";
                    strTemp = dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyStkCode));              //股票代碼
                    strResult += FilterBreakChar(strTemp) + ",";
                    byTemp = dataGetter.GetByte();                                                      //索引值
                    strResult += byTemp.ToString() + ",";

                    switch (byTemp)
                    {
                        case 50:
                            intTemp = dataGetter.GetInt();                                              //第一買價
                            strResult += intTemp.ToString() + ",";
                            intTemp = dataGetter.GetInt();                                              //第二買價
                            strResult += intTemp.ToString() + ",";
                            intTemp = dataGetter.GetInt();                                              //第三買價
                            strResult += intTemp.ToString() + ",";
                            intTemp = dataGetter.GetInt();                                              //第四買價
                            strResult += intTemp.ToString() + ",";
                            intTemp = dataGetter.GetInt();                                              //第五買價
                            strResult += intTemp.ToString() + ",";

                            uintTemp = dataGetter.GetUInt();                                            //第一買量
                            strResult += uintTemp.ToString() + ",";
                            uintTemp = dataGetter.GetUInt();                                            //第二買量
                            strResult += uintTemp.ToString() + ",";
                            uintTemp = dataGetter.GetUInt();                                            //第三買量
                            strResult += uintTemp.ToString() + ",";
                            uintTemp = dataGetter.GetUInt();                                            //第四買量
                            strResult += uintTemp.ToString() + ",";
                            uintTemp = dataGetter.GetUInt();                                            //第五買量
                            strResult += uintTemp.ToString() + ",";

                            intTemp = dataGetter.GetInt();                                              //第一賣價
                            strResult += intTemp.ToString() + ",";
                            intTemp = dataGetter.GetInt();                                              //第二賣價
                            strResult += intTemp.ToString() + ",";
                            intTemp = dataGetter.GetInt();                                              //第三賣價
                            strResult += intTemp.ToString() + ",";
                            intTemp = dataGetter.GetInt();                                              //第四賣價
                            strResult += intTemp.ToString() + ",";
                            intTemp = dataGetter.GetInt();                                              //第五賣價
                            strResult += intTemp.ToString() + ",";

                            uintTemp = dataGetter.GetUInt();                                            //第一賣量
                            strResult += uintTemp.ToString() + ",";
                            uintTemp = dataGetter.GetUInt();                                            //第二賣量
                            strResult += uintTemp.ToString() + ",";
                            uintTemp = dataGetter.GetUInt();                                            //第三賣量
                            strResult += uintTemp.ToString() + ",";
                            uintTemp = dataGetter.GetUInt();                                            //第四賣量
                            strResult += uintTemp.ToString() + ",";
                            uintTemp = dataGetter.GetUInt();                                            //第五賣量
                            strResult += uintTemp.ToString();
                            break;
                    }

                    strResult += "\r\n";
                }
            }
            catch
            {
                strResult = "";
            }
            return strResult;
        }
        /// <summary>
        /// 即時回報(訂閱結果）
        /// </summary>
        /// <param name="abyData"></param>
        /// <returns></returns>
        public string FunRealReport_Out(byte[] abyData)
        {
            string strResult = "";
            try
            {
                RR_RealReport.ParentStruct_Out struParentOut = new RR_RealReport.ParentStruct_Out();

                YuantaDataHelper dataGetter = new YuantaDataHelper(enumLng);
                dataGetter.OutMsgLoad(abyData);

                strResult += "===============\r\n即時回報(訂閱結果)\r\n===============\r\n";

                strResult += "帳號:" + dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyAccount)) + ",";

                strResult += "回報類別:" + String.Format("{0}", dataGetter.GetByte()) + ",";

                strResult += "委託單號" + dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyOrderNo)) + ",";

                strResult += String.Format("{0}", dataGetter.GetByte()) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyCompanyNo)) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyStkName)) + ",";

                TYuantaDate yuantaDate = dataGetter.GetTYuantaDate();
                strResult += String.Format("{0}/{1}/{2}", yuantaDate.ushtYear, yuantaDate.bytMon, yuantaDate.bytDay) + ",";

                TYuantaTime yuantaTime = dataGetter.GetTYuantaTime();
                strResult += String.Format("{0}:{1}:{2}.{3}", yuantaTime.bytHour, yuantaTime.bytMin, yuantaTime.bytSec, yuantaTime.ushtMSec) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyOrderType)) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyBS)) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyPrice)) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyTouchPrice)) + ",";

                strResult += String.Format("{0}", dataGetter.GetInt()) + ",";

                strResult += String.Format("{0}", dataGetter.GetInt()) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyOpenOffsetKind)) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyDayTrade)) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyOrderCond)) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyOrderErrorNo)) + ",";

                strResult += String.Format("{0}", dataGetter.GetByte()) + ",";

                strResult += String.Format("{0}", dataGetter.GetByte()) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyBasketNo)) + ",";

                strResult += String.Format("{0}", dataGetter.GetByte()) + ",";

                strResult += String.Format("{0}", dataGetter.GetByte()) + ",";

                strResult += String.Format("{0}", dataGetter.GetByte()) + ",";

                strResult += String.Format("{0}", dataGetter.GetByte()) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyBelongStkCode)) + ",";

                strResult += dataGetter.GetUInt().ToString() + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyPriceType)) + ",";

                strResult += dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyStkErrCode));
                //----------
                strResult += "\r\n";
            }
            catch
            {
                strResult = "";
            }
            return strResult;
        }
    }
}
