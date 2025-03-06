using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;

namespace RealReport
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
    //母結構(Output)
    public struct ParentStruct_Out
    {
        public uint uintCount;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct_Out
    {
        public TByte22 abyAccount;
        public byte bytRptType;
        public TByte20 abyOrderNo;
        public byte byMarketNo;
        public TByte20 abyCompanyNo;
        public TByte20 abyStkName;
        public TYuantaDate struOrderDate;
        public TYuantaTime struOrderTime;
        public TByte3 abyOrderType;
        public TByte abyBS;
        public TByte14 abyPrice;
        public TByte14 abyTouchPrice;
        public int intBeforeQty;
        public int intOrderQty;
        public TByte abyOpenOffsetKind;
        public TByte abyDayTrade;
        public TByte abyOrderCond;
        public TByte4 abyOrderErrorNo;
        public byte bytTradeKind;
        public byte bytAPCode;
        public TByte32 abyBasketNo;
        public byte byOrderStatus;
        public byte byStkType1;
        public byte byStkType2;
        public byte byBelongMarketNo;
        public TByte12 abyBelongStkCode;
        public uint uintSeqNo;
        public TByte abyPriceType;
        public TByte5 abyStkErrCode;
    }
}
