using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;
using System.Net;

namespace RR_RealReportMerge
{
    /// <summary>
    /// StructList 的摘要描述。
    /// </summary>
    /// 
    
    //--------------------
    //母結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_Out
    
    {
        public TByte22 abyAccount;
        public byte bytRptFlag;
        public TByte20 abyOrderNo;
        public byte byMarketNo;
        public TByte20 abyCompanyNo;
        public TYuantaDate struOrderDate;
        public TYuantaTime struOrderTime;
        public TByte3 abyOrderType;
        public TByte abyBS;
        public TByte14 abyOrderPrice;
        public TByte14 abyTouchPrice;
        public TByte14 abyLastDealPrice;
        public TByte14 abyAvgDealPrice;
        public int intBeforeQty;
        public int intOrderQty;
        public int intOkQty;
        public TByte abyOpenOffsetKind;
        public TByte abyDayTrade;
        public TByte abyOrderCond;
        public TByte4 abyOrderErrorNo;
        public byte byAPCode;
        public short shtOrderStatus;
        public byte byLastOrderStatus;
        public TByte20 abyCompanyName;
        public TByte20 abyTradeCode;
        public uint uintStrikePrice;
        public TByte32 abyBasketNo;
        public byte byStkType1;
        public byte byStkType2;
        public byte byBelongMarketNo;
        public TByte12 abyBelongStkCode;
        public TByte abyPriceType;
        public TByte5 abyStkErrCode;
    }
}
