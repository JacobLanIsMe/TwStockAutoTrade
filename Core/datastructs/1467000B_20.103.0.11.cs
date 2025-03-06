using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;

namespace StoStoreSummaryGroup
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
        public short shtTradeKind;	        //交易種類
        public byte byMarketNo;             //市場代碼
        public TByte20 abyMarketName;		//市場名稱
        public TByte12 abyStkCode;		    //股票代碼
        public TByte20 abyStkName;			//股票名稱
        public int intStockNos;				//股數
        public int intPrice;				//成交均價
        public long lngCost;				//持有成本
        public int intInterest;				//預估利息
        public int intBuyNotInNos;          //買進未入帳股數
        public int intSellNotInNos;         //賣出未入帳股數
        public int intCanOrderQty;          //今日可下單股數
        public int intLoan;                 //資保證金/券擔保價品
        public int intTaxRate;              //交易稅率
        public uint uintLotSize;            //交易單位
        public int intMarketPrice;          //市價
        public short shtDecimal;            //小數位數
        public byte byStkType1;             //屬性1
        public byte byStkType2;             //屬性2
        public int intBuyPrice;				//買價
        public int intSellPrice;			//賣價
        public int intUpStopPrice;          //漲停價
        public int intDownStopPrice;        //跌停價
        public uint uintPriceMultiplier;    //計價倍數
        public TByte3 abyTradeCurrency;     //報價幣別
	    public int intCDQTY;        	    //借貸股數
    }

    //--------------------
    //母結構2(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_Out2
    {
        public uint uintCount;
    }
    //--------------------
    //子結構2(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct_Out2
    {
        public TByte22 abyAccount;          //帳號
        public TByte3 abyCurrencyType;      //幣別
        public byte byMarketNo;             //市場代碼
        public TByte20 abyMarketName;		//市場名稱
        public TByte12 abyStkCode;		    //股票代碼
        public TByte20 abyStkName;			//股票名稱
        public TByte40 abyStkFullName;		//股票全名
        public int intStockQty;				//庫存股數
        public int intTradingQty;			//可交易股數
        public int intPrice;				//成交均價
        public long lngCost;				//持有成本
        public int intCloseRate;			//匯率
        public byte byRateKind;             //匯率運算模式
        public uint uintLotSize;            //交易單位
        public int intMarketPrice;			//市價
        public short shtDecimal;            //小數位數
        public int intBuyPrice;				//買價
        public int intSellPrice;			//賣價        
    }
}
