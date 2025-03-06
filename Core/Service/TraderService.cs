using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Service
{
    public class TraderService : ITraderService
    {
        YuantaOneAPITrader objYuantaOneAPI = new YuantaOneAPITrader();
        enumLangType enumLng = enumLangType.NORMAL;
        string strLoginAccount = "";
        private readonly TradeConfig tradeConfig;
        private readonly ITradeRepository _tradeRepository;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger _logger;
        private readonly enumEnvironmentMode _enumEnvironmentMode;
        public TraderService(IConfiguration config, ITradeRepository tradeRepository, ICandidateRepository candidateRepository, IDateTimeService dateTimeService, ILogger logger)
        {
            tradeConfig = config.GetSection("TradeConfig").Get<TradeConfig>();
            string environment = config.GetValue<string>("Environment").ToUpper();
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _tradeRepository = tradeRepository;
            _candidateRepository = candidateRepository;
            _dateTimeService = dateTimeService;
            _logger = logger;
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
        }
        public async Task Trade()
        {
            List<Trade> stockHoldingList = await _tradeRepository.GetStockHolding();
            List<Candidate> candidateList = await _candidateRepository.GetActiveCandidate();
            Login();
            Logout();
        }
        private bool Login()
        {
            objYuantaOneAPI.Open(_enumEnvironmentMode);
            string account = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockAccount") : "S98875005091";
            string password = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockPassword") : "1234";
            bool loginCheck = objYuantaOneAPI.Login(account, password);
            return loginCheck;
        }
        private void Logout()
        {
            objYuantaOneAPI.LogOut();
            objYuantaOneAPI.Close();
            objYuantaOneAPI.Dispose();
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
                        break;
                    case 1: //代表為RQ/RP 所回應的
                        switch (strIndex)
                        {
                            case "Login":       //一般/子帳登入
                                strResult = FunAPILogin_Out((byte[])objValue);
                                break;
                            default:           //不在表列中的直接呈現訊息
                                {
                                    if (strIndex == "")
                                        strResult = Convert.ToString(objValue);
                                    else
                                        strResult = String.Format("{0},{1}", strIndex, objValue);
                                }
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
                _logger.Error(ex, "Error in objApi_OnResponse");
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

                        //cboAccountList.BeginInvoke(new Action(() =>
                        //{
                        //    if (this.cboAccountList.Items.IndexOf((object)strLoginAccount) < 0)
                        //    {
                        //        this.cboAccountList.Items.Add((object)strLoginAccount);
                        //        this.cboAccountList.SelectedItem = this.cboAccountList.Items[this.cboAccountList.Items.Count - 1];
                        //    }
                        //}));
                    }
                    else
                    {
                        strLoginAccount = "";
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
    }
}
