using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;

namespace SgnAPILogin
{
	/// <summary>
	/// 母結構(Input)
	/// </summary>
	[StructLayout(LayoutKind.Sequential,Pack=1)]
	public struct ParentStruct_In
	{
        public TByte22 abyAccount;      //帳號	TByte22
        public TByte20 abyPWD;          //密碼	TByte20
	}
	/// <summary>
	/// 輸出的母結構
	/// </summary>
	[StructLayout(LayoutKind.Sequential,Pack=1)]
	public struct ParentStruct_Out
	{
        public TByte5 abyMsgCode;	    //訊息代碼	TByte5
        public TByte50 abyMsgContent;	//中文訊息	TByte50
        public uint uintCount;	        //筆數	Uint
	}

	/// <summary>
	/// 現貨子結構(Output)
	/// </summary>
	[StructLayout(LayoutKind.Sequential,Pack=1)]
    public struct ChildStruct_Out
	{
		public TByte22 abyAccount;	    //帳號	TByte22
        public TByte12 abySubAccName;	//分戶名稱	TByte12
        public TByte14 abyInvesotrID;	//身分證字號/統編	TByte14
        public short shtSellerNo;	    //營業員代碼	short
	}

}
