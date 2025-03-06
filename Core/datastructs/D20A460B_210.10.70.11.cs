using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;
using System.Net;

namespace RR_WatchList
{
    //母結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_Out
    {
        public TByte22 abyKey;
        public byte byMarketNo;
        public TByte12 abyStkCode;
        public byte byIndexFlag;
    }
    //--------------------
    //母結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_Out1
    {
        public int intValue;
    }
    //母結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_Out2
    {
        public int intBuyPrice;
        public int intSellPrice;

    }
    //母結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_Out3
    {
        public TYuantaTime struTime;
        public uint uintTotalOutVol;
        public uint uintTotalInVol;
        public int intDealPrice;
        public uint uintVol;
        public uint uintTotalVol;
        public uint uintTotalDealAmt;
    }
}
