using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;
using System.ComponentModel;

namespace RptOrderTradeReportGroupIV
{
    /// <summary>
    /// 母結構(Input)-Count
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_In
    {
        public TByte abyNoListCancel;			//是否列出取銷單
        public uint uintCount;
    }

    /// <summary>
    /// 現貨子結構(Input)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct_In
    {
        public TByte22 abyAccount;
    }

    /// <summary>
    /// 輸母結構(Input)-Count
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct1_Out
    {
        public uint uintCount;
    }

    /// <summary>
    /// 現貨子結構(Output)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct1_Out
    {
        public TByte22 abyAccount;	        //帳號
        public TYuantaDate struTradeYMD;	//交易日
        public byte byMarketNo;             //市場代碼
        public TByte30 abyMarketName;		//市場名稱
        public TByte12 abyCompanyNo;		//股票代碼
        public TByte30 abyStkName;			//股票名稱
        public short shtOrderType;			//委託種類
        public TByte abyBS;					//買賣別
        public long lngPrice;				//價位
        public TByte abyPriceFlag;			//價格委託種類 char(1) (M-市價, L-限價)        
        public int intBeforeQty;			//前一次委託量
        public int intAfterQty;			    //目前委託量
        public int intOkQty;				//成交量
        public short shtOrderStatus;		//狀態碼
        public TYuantaDate struAcceptDate;	//接單日期
        public TYuantaTime struAcceptTime;	//接單時間
        public TByte5 abyOrderNo;			//委託單編號
        public TByte5 abyOrderErrorNo;		//錯誤碼
        public TByte120 abyEmError;			//錯誤訊息
        public short shtSeller;				//營業員編號
        public TByte3 abyChannel;			//Channel
        public short shtAPCode;				//APCode
        public int intOTax;					//O_TAX
        public int intOCharge;				//O_Charge
        public int intODueAmt;				//應收付
        public TByte abyCancelFlag;			//可取消Flag
        public TByte abyReduceFlag;			//可減量Flag
        public TByte abyTraditionFlag;      //傳統單Flag	TByte	1
        public TByte10 abyBasketNo;
        public TByte3 abyTradeCurrency;     // 幣別
        public TByte abyTime_in_Force;      //委託效期 char(1) R:當日有效 I:IOC F:FOK
        public TByte abyOrder_Success;      //委託成功旗標 CHAR(1) 
        public TByte abyReduce_Flag;        //本委託下單是否被減量 CHAR(1) 
        public TByte abyChg_Prz_Flag;       //本委託下單是否進行改價 CHAR(1) 
        public TByte abyTSE_Cancel;         //本委託下單是否被交易所主動刪單 CHAR(1)
        public int intCancelQty;		    //取消數量
        public int intOR_Qty;   		    //原委託量        
        public TYuantaDate struUpdateDate;	//更新日期
        public TYuantaTime struUpdateTime;	//更新時間
    }
    /// <summary>
    /// 輸母結構(Input)-Count
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct2_Out
    {
        public uint uintCount;
    }

    /// <summary>
    /// 現貨子結構(Output)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct2_Out
    {
        public TByte22 abyAccount;	        // 帳號
        public byte byMarketNo;             // 市場代碼
        public TByte30 abyMarketName;       // 市場名稱
        public TByte12 abyCompanyNo;        // 股票代碼
        public TByte30 abyStkName;			// 股票名稱
        public short shtOrderType;			// 委託種類
        public TByte abyBS;					// 買賣別
        public int intOKStkNos;				// 成交量
        public long lngOPrice;				// 委託價
        public long lngSPrice;				// 成交價
        public TYuantaDateTime struDateTime;// 成交年月日時分秒
        public TByte5 abyOrderNo;			// 委託單編號
        public TByte3 abyTradeCurrency;     // 幣別
        public TByte abyPrice_Flag;         // 委託價位Flag
        public short shtExchange_Code;      // 委託別 0-一般委託 1-鉅額 2-零股 4-盤後定價 5 盤中零股
    }
    //母結構(Output)
    public struct ParentStruct3_Out
    {
        public uint uintCount; //輸出筆數
    }
    //--------------------
    //子結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct3_Out
    {
        public TByte22 abyAccount;          //帳號
        public TYuantaDate struTradeDate;   //交易日期
        public byte byMarketNo;             //市場代碼
        public TByte30 abyMarketName;		//市場名稱
        public TByte7 abyCommodityID1;      //商品名稱1
        public int intSettlementMonth1;     //商品月份1
        public int intStrikePrice1;         //履約價1
        public TByte abyBuySellKind1;       //買賣別1
        public TByte7 abyCommodityID2;      //商品名稱2
        public int intSettlementMonth2;     //商品月份2
        public int intStrikePrice2;         //履約價2
        public TByte abyBuySellKind2;       //買賣別2
        public TByte abyOpenOffsetKind;     //新/平倉
        public TByte abyOrderCondition;	    //委託條件	
        public TByte10 abyOrderPrice;       //委託價
        public int intBeforeQty;            //改量前委託口數
        public int intAfterQty;             //改量後委託口數
        public int intOKQty;                //成交口數
        public short shtStatus;             //委託狀態
        public TYuantaDate struAcceptDate;	//接單日期
        public TYuantaTime struAcceptTime;	//接單日期
        public TByte10 abyErrorNo;          //錯誤代碼
        public TByte120 abyErrorMessage;    //錯誤訊息
        public TByte5 abyOrderNO;           //委託單號
        public TByte abyProductType;        //商品種類
        public ushort ushtSeller;           //營業員代碼
        public long lngTotalMatFee;         //手續費
        public long lngTotalMatExchTax;     //證交稅
        public long lngTotalMatPremium;     //寶來應收付
        public TByte abyDayTradeID;         //當沖註記
        public TByte abyCancelFlag;			//可取消Flag
        public TByte abyReduceFlag;			//可減量Flag
        public TByte30 abyStkName1;			//商品名稱1
        public TByte30 abyStkName2;			//商品名稱2
        public TByte abyTraditionFlag;      //傳統單Flag	TByte	1
        public TByte20 abyTRID;	            //商品代碼
        public TByte3 abyCurrencyType;      //交易幣別
        public TByte3 abyCurrencyType2;     //交割幣別
        public TByte10 abyBasketNo;         //BasketNo
        public byte byMarketNo1;            //市場代碼1
        public TByte12 abyStkCode1;         //行情股票代碼1
        public byte byMarketNo2;            //市場代碼2
        public TByte12 abyStkCode2;         //行情股票代碼2
    }
    //母結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct4_Out
    {
        public uint uintCount; //輸出筆數
    }
    //--------------------
    //子結構(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct4_Out
    {
        public TByte22 abyAccount;              //帳號
        public byte byMarketNo;                 //市場代碼
        public TByte30 abyMarketName;			//市場名稱
        public TByte7 abyCommodityID1;          //商品名稱1
        public int intSettlementMonth1;         //商品月份1
        public TByte abyBuySellKind1;           //買賣別1
        public int intMatchQty;                 //成交口數
        public long lngMatchPrice1;             //成交價1
        public long lngMatchPrice2;             //成交價2
        public TYuantaTime struMatchTime;       //成交時間
        public TYuantaDate struMatchDate;       //成交日期
        public TByte5 abyOrderNo;               //委託單號
        public int intStrikePrice1;             //履約價1
        public TByte7 abyCommodityID2;          //商品名稱2
        public int intSettlementMonth2;         //商品月份2
        public TByte abyBuySellKind2;           //買賣別2
        public int intStrikePrice2;             //履約價2
        public TByte abyRecType;                //單式單/複式單
        public TByte abyProductType;            //商品種類
        public long intOrderPrice;              //委託價
        public TByte30 abyStkName1;				//商品名稱1
        public TByte30 abyStkName2;				//商品名稱2
        public TByte abyDayTradeID;             //當沖註記
        public long lngSprMatchPrice;           //複式單成交價
        public TByte20 abyTRID;	                //商品代碼
        public TByte3 abyCurrencyType;          //交易幣別
        public TByte3 abyCurrencyType2;         //交割幣別
        public TByte abySub_No;                 //子成交序號
    }
    /// <summary>
    /// 輸母結構(Input)-Count
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct5_Out
    {
        public uint uintCount;
    }

    /// <summary>
    /// 現貨子結構(Output)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct5_Out
    {
        public TByte22 abyAccount;	                //帳號
        public TYuantaDate struTradeYMD;		    //交易日
        public byte byMarketNo;                     //市場代碼
        public TByte30 abyMarketName;			    //市場名稱
        public TByte12 abyCompanyNo;			    //股票代碼
        public TByte30 abyStkName;			        //股票名稱
        public TByte abyBS;					        //買賣別
        public TByte3 abyCurrencyType;              //交易幣別
        public long lngPrice;				        //價位
        public TByte3 abyPriceType;			        //價格型態
        public int intOrderQty;				        //委託量
        public int intMatchQty;				        //成交量
        public short shtOrderStatus;		        //狀態碼
        public TYuantaDateTime struOrderTime;		//委託時間
        public TByte3 abyOrderType;			        //委託單型態
        public TByte7 abyOrderNo;			        //委託單編號
        public int intFee;                          //手續費
        public long lngAMT;                         //應收付金額
        public TByte8 abyOrderErrorNo;		        //錯誤碼
        public TByte180 abyEmError;			        //錯誤訊息
        public TByte3 abyCurrencyType2;             //交割幣別
        public TByte abyCancelFlag;					//可取消Flag
        public TByte abyReduceFlag;					//可減量Flag
        public TByte abyTraditionFlag;              //傳統單Flag	TByte	1
        public TByte abySettleType;                 //交割方式
        public TByte10 abyBasketNo;                 //BasketNo
    }
    /// <summary>
    /// 輸母結構(Input)-Count
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct6_Out
    {
        public uint uintCount;
    }

    /// <summary>
    /// 現貨子結構(Output)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct6_Out
    {
        public TByte22 abyAccount;	//帳號
        public byte byMarketNo;                     //市場代碼
        public TByte30 abyMarketName;			    //市場名稱
        public TByte12 abyCompanyNo;			    //股票代碼
        public TByte30 abyStkName;			        //股票名稱
        public TByte abyBS;					        //買賣別
        public TByte3 abyCurrencyType;              //交易幣別
        public int intMatchQty;				        //成交量
        public long lngOrderPrice;				    //價位
        public long lngMatchPrice;				    //價位
        public TYuantaDateTime struDateTime;        //成交時間 
        public int intFee;                          //手續費
        public TByte7 abyOrderNo;			        //委託單編號
        public long lngSettlementAMT;               //成交金額
        public TByte3 abyCurrencyType2;             //交割幣別
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct7_Out
    {
        public uint uintCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct7_Out
    {
        public TByte22 abyAccount;              //帳號
        public TYuantaDate struTradeDate;       //交易日期
        public byte byMarketNo;                 //市場代碼
        public TByte30 abyMarketName;			//市場名稱
        public TByte7 abyCommodityID;           //商品名稱
        public int intSettlementMonth;          //商品月份
        public TByte30 abyStkName;              //商品名稱
        public TByte abyBuySell;                //買賣別
        public TByte3 abyOrderType;             //委託方式
        public TByte14 abyOdrPrice;             //委託價
        public TByte14 abyTouchPrice;           //停損執行價
        public int intOrderQty;                 //委託口數
        public int intMatchQty;                 //成交口數
        public short shtOrderStatus;            //狀態碼
        public TYuantaDate struAcceptDate;		//接單日期
        public TYuantaTime struAcceptTime;		//接單日期
        public TByte10 abyErrorNo;		        //錯誤碼
        public TByte120 abyErrorMessage;	    //錯誤訊息
        public TByte8 abyOrderNo;               //委託書編號
        public TByte abyDayTradeID;             //當沖註記
        public TByte abyCancelFlag;             //可取消Flag
        public TByte abyReduceFlag;             //可減量Flag
        public long lngUtPrice;                 //委託價格整數位
        public int intUtPrice2;                 //委託價格分子
        public int intMinPrice2;                //委託價格分母
        public long lngUtPrice4;                //停損執行價整數位
        public int intUtPrice5;                 //停損執行價格分子
        public int intUtPrice6;                 //停損執行價格分母
        public TByte abyTraditionFlag;          //傳統單Flag	TByte	1
        public TByte10 abyBasketNo;             //Basketno
        public byte byMarketNo1;                //市場代碼1
        public TByte12 abyStkCode1;             //行情股票代碼1
        public TByte3 abyCurrencyType;          //交易幣別
        public TByte3 abyCurrencyType2;         //交割幣別
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct8_Out
    {
        public uint uintCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct8_Out
    {
        public TByte22 abyAccount;              //帳號
        public byte byMarketNo;                 //市場代碼
        public TByte30 abyMarketName;			//市場名稱
        public TByte7 abyCommodityID;           //商品名稱
        public int intSettlementMonth;          //商品月份
        public TByte30 abyStkName;              //商品名稱
        public TByte abyBuySell;                //買賣別
        public int intMatchQty;                 //成交口數
        public TByte14 abyOdrPrice;             //委託價
        public TByte14 abyMatchPrice;           //成交價
        public TYuantaDate struMatchDate;		//成交日期
        public TYuantaTime struMatchTime;		//成交日期
        public TByte8 abyOrderNo;               //委託書編號
        public TByte3 abyCurrencyType;          //交易幣別
        public TByte3 abyCurrencyType2;         //交割幣別
    }
}
