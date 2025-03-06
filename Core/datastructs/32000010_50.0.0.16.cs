using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;

namespace RP_WatchListAll
{
    /// <summary>
    /// StructList 的摘要描述。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_In
    {
        public uint uintCount;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct_In
    {
        public byte byMarketNo;
        public TByte12 abyStkCode;
    }
    //--------------------
    //母結構(Output)
    public struct ParentStruct_Out
    {
        public uint uintCount;
    }
    //--------------------

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct_Out
    {
        public byte byMarketNo;
        public TByte12 abyStkCode;
        public TByte20 abyStkName;
        public int intYstPrice;
        public int intOpenRefPrice;
        public int intUpStopPrice;
        public int intDownStopPrice;
        public uint uintYstVol;
        public TByte20 abyExtName;
        public short shtDecimal;
        public byte bytCreditPercent;
        public byte bytLenBondPercent;
        public int intOpenPrice;
        public int intHighPrice;
        public int intLowPrice;
        public int intBuyPrice;
        public uint uintTotalOutVol;
        public int intSellPrice;
        public uint uintTotalInVol;
        public int intDealPrice;
        public uint uintTotalDealAmt;
        public byte bytVolFlag;
        public uint uintVol;
        public uint uintTotalVol;
        public uint uintFixedPriceVol;
        public uint uintReserveVol;
        public int intSettlementPrice;
        public int intHiContractPrice;
        public int intLoContractPrice;
        public uint uintOrderBuyCount;
        public uint uintOrderBuyQty;
        public uint uintOrderSellCount;
        public uint uintOrderSellQty;
        public uint uintDealBuyCount;
        public uint uintDealSellCount;
        public uint uintVolatility;
        public TYuantaTime struTime;
        public sbyte sbytTimeDiff;
        public byte bytStkType2;
        public int intReserveVolDiff;
        public TByte2 abyBelongCode;
        public TByte20 abyIndustryName;
        public int intPrincipalPercent;
        public short shtUpDownDay;
        public uint uintBidQty;
        public uint uintAskQty;
        public byte byPriceTrends;
        public int intEstDealPrice;
        public uint uintEstDealVol;
        public byte byEstDealVolFlag;
    }
}
