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
        private readonly ILogger _logger;
        private readonly string _futureAccount;
        private readonly string _futurePassword;
        private readonly string _targetFutureCode;
        private readonly int _maxOrderQuantity;
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
                _logger.Error("Trade() 已被取消。");
                throw new Exception("Trade() 已被取消。");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
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
                            case "210.10.70.11":    //Watchlist報價表(指定欄位)
                                strResult = FunRealWatchlist_Out((byte[])objValue);
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
                // 訂閱國內期貨報價
            }
            else
            {
                _cts.Cancel();
            }
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
        /// Watchlist指定欄位 (即時訂閱結果)
        /// </summary>
        /// <param name="abyData"></param>
        /// <returns></returns>
        private string FunRealWatchlist_Out(byte[] abyData)
        {
            string strResult = "";
            try
            {
                RR_WatchList.ParentStruct_Out struParentOut = new RR_WatchList.ParentStruct_Out();
                YuantaDataHelper dataGetter = new YuantaDataHelper(enumLng);
                dataGetter.OutMsgLoad(abyData);

                int intCheck = 1;
                if (intCheck == 1)
                {
                    strResult += "WatchList指定欄位訂閱結果: \r\n";
                    string strTemp = "";
                    byte byTemp = new byte();
                    int intTemp = 0;

                    strTemp = dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyKey));          //鍵值
                    byTemp = dataGetter.GetByte();                                              //市場代碼
                    strResult += byTemp.ToString() + ",";
                    strTemp = dataGetter.GetStr(Marshal.SizeOf(struParentOut.abyStkCode));      //股票代碼
                    strResult += FilterBreakChar(strTemp) + ",";
                    byTemp = dataGetter.GetByte();                                              //索引值
                    strResult += byTemp.ToString() + ",";
                    intTemp = dataGetter.GetInt();                                              //資料值
                    strResult += intTemp.ToString() + ",";

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
