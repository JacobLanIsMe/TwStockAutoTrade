using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;

namespace StoOVFutStoreSummaryIV
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
        public byte abyKind;                //委託種類
        public TByte20 abyTrid;             //商品代碼
        public byte abyBS;                  //買賣別
        public int intQty;                  //未平倉口數
        public long lngAmt;				    //總成交點數
        public TByte6 abyCommodityID1;      //商品名稱1
        public byte abyCallPut1;            //買賣權1
        public TByte18 abyProductCName1;    //商品中文名稱1
        public int intStrikePrice1;         //履約價1
        public TByte6 abyCommodityID2;      //商品名稱2
        public byte abyCallPut2;            //買賣權2
        public int intSettlementMonth2;     //交易月份2
        public TByte18 abyProductCName2;    //商品中文名稱2
        public int intStrikePrice2;         //履約價2
        public int intFee;                  //手續費
        public TByte3 abyCurrencyType;      //幣別
        public byte abyDayTradeID;          //當沖註記
        public byte abyBS1;                 //買賣別1
        public byte abyBS2;                 //買賣別2
        public byte abyOptProdKind1;        //選擇權商品種類1
        public byte abyOptProdKind2;        //選擇權商品種類2
        public byte byMarketNo1;            //市場代碼1
        public TByte12 abyStkCode1;         //行情股票代碼1
        public byte byMarketNo2;            //市場代碼2
        public TByte12 abyStkCode2;         //行情股票代碼2
        public int intBuyPrice1;            //買入價1
        public int intSellPrice1;           //賣出價1
        public int intMarketPrice1;         //市價1
        public int intBuyPrice2;            //買入價2
        public int intSellPrice2;           //賣出價2
        public int intMarketPrice2;         //市價2
        public short shtDecimal;            //小數位數
        public uint uintTickDiff;           //檔差
    }
}
