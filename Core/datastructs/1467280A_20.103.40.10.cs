using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;

namespace StoOVFutStoreGroup
{
    //--------------------
    //母結構(Input)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_In
    {
        public uint uintCount;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct_In
    {
        public TByte22 abyAccount;
    }

    //--------------------
    //母結構1(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_Out1
    {
        public uint uintCount;
    }
    //--------------------
    //子結構1(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct_Out1
    {
        public TByte22 abyAccount;          //帳號
        public TYuantaDate struTradeDate;   //交易日期
        public byte abyBuySell;             //買賣別
        public short shtQty;                //口數
        public TByte7 abyCommodityID;		//商品代碼
        public int intSettlementMonth;		//商品年月
        public int intStrikePrice;			//履約價
        public byte abyCallPut;             //買賣權
        public TByte20 abyPrice;			//價格
        public TByte20 abyMktPrice;			//市價
        public long lngPrtlos;				//浮動損益
    }
}
