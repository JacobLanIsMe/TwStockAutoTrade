using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;
using System.ComponentModel;

namespace RptOrderTradeReportGroup
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
        [Description("帳號")]
        public TByte22 abyAccount;	        //帳號
        [Description("交易日")]
        public TYuantaDate struTradeYMD;    //交易日
        [Description("市場代碼")]
        public byte byMarketNo;             //市場代碼
        [Description("市場名稱")]
        public TByte20 abyMarketName;		//市場名稱
        [Description("股票代碼")]
        public TByte12 abyCompanyNo;		//股票代碼
        [Description("股票名稱")]
        public TByte20 abyStkName;			//股票名稱
        [Description("委託種類")]
        public short shtOrderType;			//委託種類
        [Description("買賣別")]
        public TByte abyBS;					//買賣別
        [Description("價位")]
        public long lngPrice;				//價位
        [Description("價格種類")]
        public TByte abyPriceFlag;			//價格種類
        [Description("前一次委託量")]
        public int intBeforeQty;			//前一次委託量
        [Description("目前委託量")]
        public int intAfterQty;			    //目前委託量
        [Description("成交量")]
        public int intOkQty;				//成交量
        [Description("委託狀態")]
        public short shtOrderStatus;		//委託狀態
        [Description("委託日期")]
        public TYuantaDate struAcceptDate;	//委託日期
        [Description("委託時間")]
        public TYuantaTime struAcceptTime;	//委託時間
        [Description("委託單編號")]
        public TByte5 abyOrderNo;			//委託單編號
        [Description("錯誤碼")]
        public TByte5 abyOrderErrorNo;		//錯誤碼
        [Description("錯誤訊息")]
        public TByte80 abyEmError;			//錯誤訊息
        [Description("營業員編號")]
        public short shtSeller;				//營業員編號
        [Description("Channel")]
        public TByte3 abyChannel;			//Channel
        [Description("APCode")]
        public short shtAPCode;				//APCode
        [Description("O_TAX")]
        public int intOTax;					//O_TAX
        [Description("O_Charge")]
        public int intOCharge;				//O_Charge
        [Description("應收付")]
        public int intODueAmt;				//應收付
        [Description("可取消Flag")]
        public TByte abyCancelFlag;			//可取消Flag
        [Description("可減量Flag")]
        public TByte abyReduceFlag;			//可減量Flag
        [Description("傳統單Flag")]
        public TByte abyTraditionFlag;      //傳統單Flag
        [Description("BasketNo")]
        public TByte10 abyBasketNo;         // Basketno
        [Description("幣別")]
        public TByte3 abyTradeCurrency;     //幣別
        [Description("委託效期")]
        public TByte abyTime_in_Force;      //委託效期
        [Description("委託成交旗標")]
        public TByte abyOrder_Success;      //委託成交旗標
        [Description("本委託下單是否被減量")]
        public TByte abyReduce_Flag;        //本委託下單是否被減量
        [Description("本委託下單是否進行改價")]
        public TByte abyChg_Prz_Flag;       //本委託下單是否進行改價
        [Description("本委託下單是否被交易所主動刪單")]
        public TByte abyTSE_Cancel;         //本委託下單是否被交易所主動刪單
        [Description("取消數量")]
        public int intCancelQty;            //取消數量
        [Description("原委託量")]
        public int intOR_QTY;               //原委託量
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
        [Description("帳號")]
        public TByte22 abyAccount;	//帳號
        [Description("市場代碼")]
        public byte byMarketNo;             //市場代碼
        [Description("市場名稱")]
        public TByte20 abyMarketName;       //市場名稱
        [Description("股票代碼")]
        public TByte12 abyCompanyNo;        //股票代碼
        [Description("股票名稱")]
        public TByte20 abyStkName;			// 股票名稱
        [Description("委託種類")]
        public short shtOrderType;			// 委託種類
        [Description("買賣別")]
        public TByte abyBS;					// 買賣別
        [Description("成交量")]
        public int intOKStkNos;				// 成交量
        [Description("委託價")]
        public long lngOPrice;				// 委託價
        [Description("成交價")]
        public long lngSPrice;				// 成交價
        [Description("成交年月日時分秒")]
        public TYuantaDateTime struDateTime;// 成交年月日時分秒
        [Description("委託單編號")]
        public TByte5 abyOrderNo;			// 委託單編號
        [Description("幣別")]
        public TByte3 abyTradeCurrency;     // 幣別
        [Description("價位Flag")]
        public TByte abyPrice_Flag;        // 價位Flag: 1-市價 2-限價
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
        [Description("帳號")]
        public TByte22 abyAccount;  //帳號
        [Description("交易日期")]
        public TYuantaDate struTradeDate;             //交易日期
        [Description("市場代碼")]
        public byte byMarketNo;                     //市場代碼
        [Description("市場名稱")]
        public TByte20 abyMarketName;			    //市場名稱
        [Description("商品代碼1")]
        public TByte7 abyCommodityID1;              //商品名稱1
        [Description("商品月份1")]
        public int intSettlementMonth1;             //商品月份1
        [Description("履約價1")]
        public int intStrikePrice1;                 //履約價1
        [Description("買賣別1")]
        public TByte abyBuySellKind1;               //買賣別1
        [Description("商品代碼2")]
        public TByte7 abyCommodityID2;              //商品名稱2
        [Description("商品月份2")]
        public int intSettlementMonth2;             //商品月份2
        [Description("履約價2")]
        public int intStrikePrice2;                 //履約價2
        [Description("買賣別2")]
        public TByte abyBuySellKind2;               //買賣別2
        [Description("新/平倉")]
        public TByte abyOpenOffsetKind;             //新/平倉
        [Description("委託條件")]
        public TByte abyOrderCondition;	            //委託條件	
        [Description("委託價")]
        public TByte10 abyOrderPrice;               //委託價
        [Description("改量前委託口數")]
        public int intBeforeQty;                    //改量前委託口數
        [Description("改量後委託口數")]
        public int intAfterQty;                     //改量後委託口數
        [Description("成交口數")]
        public int intOKQty;                        //成交口數
        [Description("委託狀態")]
        public short shtStatus;                     //委託狀態
        [Description("接單日期")]
        public TYuantaDate struAcceptDate;		    //接單日期
        [Description("接單時間")]
        public TYuantaTime struAcceptTime;		    //接單時間
        [Description("錯誤代碼")]
        public TByte10 abyErrorNo;                  //錯誤代碼
        [Description("錯誤訊息")]
        public TByte78 abyErrorMessage;             //錯誤訊息
        [Description("委託單號")]
        public TByte5 abyOrderNO;                   //委託單號
        [Description("商品種類")]
        public TByte abyProductType;                //商品種類
        [Description("營業員代碼")]
        public ushort ushtSeller;                   //營業員代碼
        [Description("手續費")]
        public long lngTotalMatFee;                 //手續費
        [Description("證交稅")]
        public long lngTotalMatExchTax;             //證交稅
        [Description("應收付")]
        public long lngTotalMatPremium;             //寶來應收付
        [Description("當沖註記")]
        public TByte abyDayTradeID;                 //當沖註記
        [Description("可取消Flag")]
        public TByte abyCancelFlag;					//可取消Flag
        [Description("可減量Flag")]
        public TByte abyReduceFlag;					//可減量Flag
        [Description("商品名稱1")]
        public TByte20 abyStkName1;					//商品名稱1
        [Description("商品名稱2")]
        public TByte20 abyStkName2;					//商品名稱2
        [Description("傳統單Flag")]
        public TByte abyTraditionFlag;              //傳統單Flag
        [Description("商品代碼")]
        public TByte20 abyTRID;	                    //商品代碼
        [Description("交易幣別")]
        public TByte3 abyCurrencyType;              //交易幣別
        [Description("交割幣別")]
        public TByte3 abyCurrencyType2;             //交割幣別
        [Description("BasketNo")]
        public TByte10 abyBasketNo;                 //BasketNo
        [Description("市場代碼1")]
        public byte byMarketNo1;                    //市場代碼1
        [Description("行情股票代碼1")]
        public TByte12 abyStkCode1;                 //行情股票代碼1
        [Description("市場代碼2")]
        public byte byMarketNo2;                    //市場代碼2
        [Description("行情股票代碼2")]
        public TByte12 abyStkCode2;                 //行情股票代碼2
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
        [Description("帳號")]
        public TByte22 abyAccount;              //帳號
        [Description("市場代碼")]
        public byte byMarketNo;                 //市場代碼
        [Description("市場名稱")]
        public TByte20 abyMarketName;			//市場名稱
        [Description("商品代碼1")]
        public TByte7 abyCommodityID1;          //商品名稱1
        [Description("商品月份1")]
        public int intSettlementMonth1;         //商品月份1
        [Description("買賣別1")]
        public TByte abyBuySellKind1;           //買賣別1
        [Description("成交口數")]
        public int intMatchQty;                 //成交口數
        [Description("成交價1")]
        public long lngMatchPrice1;             //成交價1
        [Description("成交價2")]
        public long lngMatchPrice2;             //成交價2
        [Description("成交時間")]
        public TYuantaTime struMatchTime;         //成交時間
        [Description("成交日期")]
        public TYuantaDate struMatchDate;         //成交日期
        [Description("委託單號")]
        public TByte5 abyOrderNo;               //委託單號
        [Description("履約價1")]
        public int intStrikePrice1;             //履約價1
        [Description("商品代碼2")]
        public TByte7 abyCommodityID2;          //商品名稱2
        [Description("商品月份2")]
        public int intSettlementMonth2;         //商品月份2
        [Description("買賣別2")]
        public TByte abyBuySellKind2;           //買賣別2
        [Description("履約價2")]
        public int intStrikePrice2;             //履約價2
        [Description("單式單/複式單")]
        public TByte abyRecType;                //單式單/複式單
        [Description("商品種類")]
        public TByte abyProductType;            //商品種類
        [Description("委託價")]
        public long lngOrderPrice;              //委託價
        [Description("商品名稱1")]
        public TByte20 abyStkName1;				//商品名稱1
        [Description("商品名稱2")]
        public TByte20 abyStkName2;				//商品名稱2
        [Description("當沖註記")]
        public TByte abyDayTradeID;             //當沖註記
        [Description("複式單成交價")]
        public long lngSprMatchPrice;           //複式單成交價
        [Description("商品代碼")]
        public TByte20 abyTRID;	                //商品代碼
        [Description("交易幣別")]
        public TByte3 abyCurrencyType;          //交易幣別
        [Description("交割幣別")]
        public TByte3 abyCurrencyType2;         //交割幣別
        [Description("子成交序號")]
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
        [Description("帳號")]
        public TByte22 abyAccount;	                //帳號
        [Description("交易日")]
        public TYuantaDate struTradeYMD;		    //交易日
        [Description("市場代碼")]
        public byte byMarketNo;                     //市場代碼
        [Description("市場名稱")]
        public TByte20 abyMarketName;			    //市場名稱
        [Description("股票代碼")]
        public TByte12 abyCompanyNo;			    //股票代碼
        [Description("股票名稱")]
        public TByte20 abyStkName;			        //股票名稱
        [Description("買賣別")]
        public TByte abyBS;					        //買賣別
        [Description("交易幣別")]
        public TByte3 abyCurrencyType;              //交易幣別
        [Description("價位")]
        public long lngPrice;				        //價位
        [Description("價格型態")]
        public TByte3 abyPriceType;			        //價格型態
        [Description("委託量")]
        public int intOrderQty;				        //委託量
        [Description("成交量")]
        public int intMatchQty;				        //成交量
        [Description("狀態碼")]
        public short shtOrderStatus;		        //狀態碼
        [Description("委託時間")]
        public TYuantaDateTime struOrderTime;		//委託時間
        [Description("委託單型態")]
        public TByte3 abyOrderType;			        //委託單型態
        [Description("委託單編號")]
        public TByte7 abyOrderNo;			        //委託單編號
        [Description("手續費")]
        public int intFee;                          //手續費
        [Description("應收付金額")]
        public long lngAMT;                         //應收付金額
        [Description("錯誤碼")]
        public TByte8 abyOrderErrorNo;		        //錯誤碼
        [Description("錯誤訊息")]
        public TByte120 abyEmError;			        //錯誤訊息
        [Description("交割幣別")]
        public TByte3 abyCurrencyType2;             //交割幣別
        [Description("可取消Flag")]
        public TByte abyCancelFlag;					//可取消Flag
        [Description("可減量Flag")]
        public TByte abyReduceFlag;					//可減量Flag
        [Description("傳統單Flag")]
        public TByte abyTraditionFlag;              //傳統單Flag	
        [Description("交割方式")]
        public TByte abySettleType;                 //交割方式
        [Description("BasketNo")]
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
        [Description("帳號")]
        public TByte22 abyAccount;	//帳號
        [Description("市場代碼")]
        public byte byMarketNo;                     //市場代碼
        [Description("市場名稱")]
        public TByte20 abyMarketName;			    //市場名稱
        [Description("股票代碼")]
        public TByte12 abyCompanyNo;			    //股票代碼
        [Description("股票名稱")]
        public TByte20 abyStkName;			        //股票名稱
        [Description("買賣別")]
        public TByte abyBS;					        //買賣別
        [Description("交易幣別")]
        public TByte3 abyCurrencyType;              //交易幣別
        [Description("成交量")]
        public int intMatchQty;				        //成交量
        [Description("委託價")]
        public long lngOrderPrice;				    //委託價
        [Description("成交價")]
        public long lngMatchPrice;				    //成交價
        [Description("成交時間")]
        public TYuantaDateTime struDateTime;          //成交時間    
        [Description("手續費")]
        public int intFee;                          //手續費
        [Description("委託單編號")]
        public TByte7 abyOrderNo;			        //委託單編號
        [Description("成交金額")]
        public long lngSettlementAMT;               //成交金額
        [Description("交割幣別")]
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
        [Description("帳號")]
        public TByte22 abyAccount;              //帳號
        [Description("交易日期")]
        public TYuantaDate struTradeDate;       //交易日期
        [Description("市場代碼")]
        public byte byMarketNo;                 //市場代碼
        [Description("市場名稱")]
        public TByte20 abyMarketName;			//市場名稱
        [Description("商品代碼")]
        public TByte7 abyCommodityID;           //商品代碼
        [Description("商品年月")]
        public int intSettlementMonth;          //商品年月
        [Description("商品名稱")]
        public TByte20 abyStkName;              //商品名稱
        [Description("買賣別")]
        public TByte abyBuySell;                //買賣別
        [Description("委託方式")]
        public TByte3 abyOrderType;             //委託方式
        [Description("委託價")]
        public TByte14 abyOdrPrice;             //委託價
        [Description("停損執行價")]
        public TByte14 abyTouchPrice;           //停損執行價
        [Description("委託口數")]
        public int intOrderQty;                 //委託口數
        [Description("成交口數")]
        public int intMatchQty;                 //成交口數
        [Description("狀態碼")]
        public short shtOrderStatus;            //狀態碼
        [Description("委託日期")]
        public TYuantaDate struAcceptDate;		//委託日期
        [Description("委託時間")]
        public TYuantaTime struAcceptTime;		//委託時間
        [Description("錯誤代碼")]
        public TByte10 abyErrorNo;		        //錯誤代碼
        [Description("錯誤訊息")]
        public TByte80 abyErrorMessage;			//錯誤訊息
        [Description("委託書編號")]
        public TByte8 abyOrderNo;               //委託書編號
        [Description("當沖註記")]
        public TByte abyDayTradeID;             //當沖註記
        [Description("可取消Flag")]
        public TByte abyCancelFlag;             //可取消Flag
        [Description("可減量Flag")]
        public TByte abyReduceFlag;             //可減量Flag
        [Description("委託價格整數位")]
        public long lngUtPrice;                 //委託價格整數位
        [Description("委託價格分子")]
        public int intUtPrice2;                 //委託價格分子 
        [Description("委託價格分母")]
        public int intMinPrice2;                //委託價格分母
        [Description("停損執行價整數位")]
        public long lngUtPrice4;                //停損執行價整數位
        [Description("停損執行價格分子")]
        public int intUtPrice5;                 //停損執行價格分子
        [Description("停損執行價格分母")]
        public int intUtPrice6;                 //停損執行價格分母
        [Description("傳統單Flag")]
        public TByte abyTraditionFlag;          //傳統單Flag
        [Description("Basketno")]
        public TByte10 abyBasketNo;             //Basketno
        [Description("市場代碼1")]
        public byte byMarketNo1;                //市場代碼1
        [Description("行情股票代碼1")]
        public TByte12 abyStkCode1;             //行情股票代碼1
        [Description("交易幣別")]
        public TByte3 abyCurrencyType;          //交易幣別
        [Description("交割幣別")]
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
        [Description("帳號")]
        public TByte22 abyAccount;              //帳號
        [Description("市場代碼")]
        public byte byMarketNo;                 //市場代碼
        [Description("市場名稱")]
        public TByte20 abyMarketName;			//市場名稱
        [Description("商品代碼")]
        public TByte7 abyCommodityID;           //商品代碼
        [Description("商品月份")]
        public int intSettlementMonth;          //商品月份
        [Description("商品名稱")]
        public TByte20 abyStkName;              //商品名稱
        [Description("買賣別")]
        public TByte abyBuySell;                //買賣別
        [Description("成交口數")]
        public int intMatchQty;                 //成交口數
        [Description("委託價")]
        public TByte14 abyOdrPrice;             //委託價
        [Description("成交價")]
        public TByte14 abyMatchPrice;           //成交價
        [Description("成交日期")]
        public TYuantaDate struMatchDate;		//成交日期
        [Description("成交時間")]
        public TYuantaTime struMatchTime;		//成交時間
        [Description("委託書編號")]
        public TByte8 abyOrderNo;               //委託書編號
        [Description("交易幣別")]
        public TByte3 abyCurrencyType;          //交易幣別
        [Description("交割幣別")]
        public TByte3 abyCurrencyType2;         //交割幣別
    }
}
