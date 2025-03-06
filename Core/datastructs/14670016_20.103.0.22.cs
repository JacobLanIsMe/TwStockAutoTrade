using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;


namespace StoStoreSummaryGroupIX
{
	/// <summary>
	/// StructList 的摘要描述。
	/// </summary>
	
	//--------------------
	//母結構(Input)
	[StructLayout(LayoutKind.Sequential,Pack=1)]
	public struct ParentStruct_In
	{
		public uint uintCount ;		//輸入筆數
	}
	//--------------------
	//子結構(Input)
	[StructLayout(LayoutKind.Sequential,Pack=1)]
	public struct ChildStruct_In
	{
		public TByte22 abyAccount;
	}
	//--------------------
    //母結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct1_Out
    {
        public uint uintCount;		//輸出筆數
    }
    //--------------------
    //子結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct1_Out	//股票庫存結構
    {
        public TByte22 abyAccount;       //帳號
        public short shtTradeKind;       //交易種類
        public byte byMarketNo;          //市場代碼
        public TByte30 abyMarketName;    //市場名稱
        public TByte12 abyStkCode;       //股票代碼
        public TByte30 abyStkName;       //股票名稱
        public long lngStockNos;	     //股數
        public long lngPrice;	         //成交均價
        public long lngCost;		     //持有成本
        public long lngInterest;		 //預估利息
        public int intBuyNotInNos;       //買進未入帳股數
        public int intSellNotInNos;      //賣出未入帳股數
        public long lngCanOrderQty;      //今日可下單股數
        public long lngLoan;		     //資保證金/券擔保價品
        public int intTaxRate;           //交易稅率
        public uint uintLotSize;         //交易單位
        public int intMarketPrice;       //市價
        public short shtDecimal;         //小數位數
        public byte byStkType1;          //屬性1
        public byte byStkType2;          //屬性2
        public int intBuyPrice;          //買價
        public int intSellPrice;         //賣價
        public int intUpStopPrice;       //漲停價
        public int intDownStopPrice;     //跌停價
        public uint uintPriceMultiplier; //計價倍數
        public TByte3 abyTradeCurrency;  //報價幣別
        public long lngCDQTY;            //借貸股數
        public long lngCanOrderOddQty;   //零股可下單股數
    }
	//母結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct ParentStruct2_Out
	{
		public uint uintCount ;		//輸出筆數
	}
	//--------------------
	//子結構(Output)
	[StructLayout(LayoutKind.Sequential,Pack=1)]
	public struct ChildStruct2_Out	//股票庫存結構
	{
        public TByte22 abyAccount;           //帳號
        public TByte3 abyCurrencyType;       //幣別
        public byte byMarketNo;              //市場代碼
        public TByte30 abyMarketName;        //市場名稱
        public TByte12 abyStkCode;           //股票代碼
        public TByte30 abyStkName;           //股票名稱
        public TByte60 abyStkFullName;       //股票全名
        public long lngStockQty;             //庫存股數
        public long lngTradingQty;           //可交易股數
        public long lngPrice;                //成交均價
        public long lngCost;                 //持有成本
        public int intCloseRate;             //匯率
        public byte byRateKind;              //匯率運算模式
        public uint uintLotSize;             //交易單位
        public int intMarketPrice;           //市價
        public short shtDecimal;             //小數位數
        public int intBuyPrice;              //買價
        public int intSellPrice;             //賣價        
	}
}
