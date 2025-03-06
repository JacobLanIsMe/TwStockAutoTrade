using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;

namespace RR_StockTick
{
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
        public TByte22 abyKey;
        public byte byMarketNo;
        public TByte12 abyStkCode;
        public uint uintSerialNo;
        public TYuantaTime struTime;
        public int intBuyPrice;
        public int intSellPrice;
        public int intDealPrice;
        public uint dwDealVol;
        public byte byInOutFlag;
        public byte byType;
    }
}
