using Core.Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Service
{
    public class YuantaService : IYuantaService
    {
        enumLangType enumLng = enumLangType.NORMAL;

        /// <summary>
        /// Watchlist指定欄位 (即時訂閱結果)
        /// </summary>
        /// <param name="abyData"></param>
        /// <returns></returns>
        public string FunRealWatchlist_Out(byte[] abyData)
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
        /// <summary>
        /// 帳號登入
        /// </summary>
        /// <param name="abyData"></param>
        /// <returns></returns>
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
                        //strLoginAccount = "";
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
