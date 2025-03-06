using YuantaShareStructList;
using System;
using System.Runtime.InteropServices;

namespace RR_FiveTick
{
    //--------------------
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

    //--------------------
    //母結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_Out2
    {
        public int intPrice1;
        public int intPrice2;
        public int intPrice3;
        public int intPrice4;
        public int intPrice5;
        public int intVol1;
        public int intVol2;
        public int intVol3;
        public int intVol4;
        public int intVol5;
    }

    //--------------------
    //母結構(Output) 
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_Out3
    {
        public int intBuyPrice1;
        public int intBuyPrice2;
        public int intBuyPrice3;
        public int intBuyPrice4;
        public int intBuyPrice5;

        public int intBuyVol1;
        public int intBuyVol2;
        public int intBuyVol3;
        public int intBuyVol4;
        public int intBuyVol5;

        public int intSellPrice1;
        public int intSellPrice2;
        public int intSellPrice3;
        public int intSellPrice4;
        public int intSellPrice5;

        public int intSellVol1;
        public int intSellVol2;
        public int intSellVol3;
        public int intSellVol4;
        public int intSellVol5;

    }
}
