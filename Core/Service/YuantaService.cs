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
        enumLangType enumLng = enumLangType.UTF8;
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
