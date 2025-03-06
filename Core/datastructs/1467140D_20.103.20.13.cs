using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;

namespace StoFutStoreClassifyIIIGroup
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
        public TByte21 abyTrid;             //商品代碼
        public byte abyBS;                  //買賣別
        public int intQty;                  //未平倉口數
        public long lngAmt;				    //總成交點數
        public int intFee;                  //手續費
        public int intTax;                  //交易稅
        public TByte3 abyCurrencyType;      //幣別
        public byte abyDayTradeID;          //當沖註記
        public TByte6 abyCommodityID1;      //商品名稱1
        public byte abyCallPut1;            //買賣權1
        public int intSettlementMonth1;     //交易月份1
        public int intStrikePrice1;         //履約價1
        public byte abyBS1;                 //買賣別1
        public TByte20 abyStkName1;         //股票名稱1
        public byte byMarketNo1;            //市場代碼1
        public TByte12 abyStkCode1;         //行情報價代碼1
        public TByte6 abyCommodityID2;      //商品名稱2
        public byte abyCallPut2;            //買賣權2
        public int intSettlementMonth2;     //交易月份2
        public int intStrikePrice2;         //履約價2
        public byte abyBS2;                 //買賣別2
        public TByte20 abyStkName2;         //股票名稱2
        public byte byMarketNo2;            //市場代碼2
        public TByte12 abyStkCode2;         //行情報價代碼2
        public int intBuyPrice1;            //買入價1
        public int intSellPrice1;           //賣出價1
        public int intMarketPrice1;         //市價1
        public int intBuyPrice2;            //買入價2
        public int intSellPrice2;           //賣出價2
        public int intMarketPrice2;         //市價2
        public short shtDecimal;            //小數位數
        public byte abyProductType1;        //商品類別1
        public byte abyProductKind1;        //商品屬性1
        public byte abyProductType2;        //商品類別2
        public byte abyProductKind2;        //商品屬性2
        public int intUpStopPrice1;         //漲停價1
        public int intDownStopPrice1;       //跌停價1
        public int intUpStopPrice2;         //漲停價2
        public int intDownStopPrice2;       //跌停價2
        public TByte12 abyStkCode1opp;      //行情股票代碼1反向
        public TByte12 abyStkCode2opp;      //行情股票代碼2反向
    }
}
