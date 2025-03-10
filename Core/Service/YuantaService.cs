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
    }
}
