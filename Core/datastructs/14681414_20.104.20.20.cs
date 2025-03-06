using System;
using System.Runtime.InteropServices;
using YuantaShareStructList;

namespace PriFutInterestStore
{
    //--------------------
    //母結構(Input)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_In
    {
        public TByte22 abyAccount;
        public byte abyType;
        public TByte3 abyCurrency;  
    }

    //--------------------
    //母結構1(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_Out1
    {
        public short shtReplyCode;          //委託結果代碼
        public TByte78 abyAdvisory;         //錯誤說明
        public byte abyType;                //型態
        public TByte3 abyCurrency;          //幣別
        public long lngEquity;				//權益數
        public long lngAllFullIm;			//全額原始保證金
        public long lngCanuseMargin;	    //可運用保證金
        public TByte9 abyRiskRate;          //權益比率
        public TByte9 abyDaytradeRisk;      //當沖風險指標
        public TByte9 abyAllRiskRate;       //風險指標
        public long lngCashForward;			//前日餘額
        public long lngOpenGlYes;           //昨日未平倉損益
        public TYuantaTime strucUpdateTime;	//風險更新時間
        public long lngAccounting;          //存/提
        public long lngFloatMargin;         //未沖銷期貨浮動損益
        public long lngFloatPremium;        //未沖銷買方選擇權市值 + 未沖銷賣方選擇權市值
        public long lngCommissionAll;       //手續費    
        public long lngTotalValue;          //權益總值
        public long lngTaxRate;             //期交稅
        public long lngAllIm;               //原始保證金
        public long lngCallMargin;          //追繳保證金
        public long lngGrantal;             //本日期貨平倉損益淨額 + 到期履約損益
        public long lngAllMm;               //維持保證金
        public long lngOrderIm;             //委託保證金
        public long lngPremium;             //權利金收入與支出
        public long lngOrderPremium;        //委託權利金
        public long lngBalance;             //本日餘額
        public long lngCanusePremium;       //可動用(出金)保證金(含抵委)
        public long lngCoveredOim;          //委託抵繳保證金
        public long lngBondAmt;             //債券實物交割款
        public long lngNobondAmt;           //債券實物不足交割款
        public long lngBondMargin;          //債券待交割保證金
        public long lngCoveredIm;           //有價證券抵繳總額
        public long lngReduceIm;            //期貨多空減收保證金
        public long lngIncreaseIm;          //加收保證金
        public long lngYTotalValue;         //昨日權益總值
        public long lngRate;                //匯率
        public byte abyBestFlag;            //客戶保證金計收方式
        public long lngGlToday;             //本日損益
        public long lngDspEquity;           //風險權益總值
        public long lngDspFloatmargin;      //未沖銷期貨風險浮動損益
        public long lngDspFloatpremium;     //未沖銷買方選擇權風險市值+未沖銷賣方選擇權風險市值
        public long lngDspIM;               //風險原始保證金
        public long lngDspRiskRate;         //盤後風險指標
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParentStruct_Out2
    {
        public uint uintCount;
    }

    //--------------------
    //子結構1(Output)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChildStruct_Out2
    {
        public TByte22 abyAccount;          //帳號
        public TByte3 abyKind;              //期權別
        public TByte21 abyTrid;             //商品代碼
        public TByte12 abyID1;              //商品組合代碼-單腳1
        public TByte6 abyCommodity1;        //商品代碼1
        public int intSettlementMonth1;     //商品月份1
        public byte abyCP1;                 //買賣權
        public int intStrikePrice1;         //履約價1
        public int intNetLotsB1;            //留倉總買1
        public int intNetLotsS1;            //留倉總賣1
        public byte byMarketNo1;            //市場代碼1
        public TByte12 abyStkCode1;         //行情報價代碼1
        public TByte20 abyStkName1;         //股票名稱1
        public short shtDecimal1;           //小數位數1
        public int intBuyPrice1;            //買入價1
        public int intSellPrice1;           //賣出價1
        public int intMarketPrice1;         //市價1
        public TByte12 abyID2;              //商品組合代碼-單腳2
        public TByte6 abyCommodity2;        //商品代碼2
        public int intSettlementMonth2;     //商品月份2
        public byte abyCP2;                 //買賣權2
        public int intStrikePrice2;         //履約價2
        public int intNetLotsB2;            //留倉總買2
        public int intNetLotsS2;            //留倉總賣2
        public byte byMarketNo2;            //市場代碼2
        public TByte12 abyStkCode2;         //行情報價代碼2
        public TByte20 abyStkName2;         //股票名稱2
        public short shtDecimal2;           //小數位數2
        public int intBuyPrice2;            //買入價2
        public int intSellPrice2;           //賣出價2
        public int intMarketPrice2;         //市價2
    }
}
