﻿using Core.Enum;
using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Service
{
    public class CrazyTraderService : ICrazyTraderService
    {
        //YuantaOneAPITrader objYuantaOneAPI = new YuantaOneAPITrader();
        //private readonly enumEnvironmentMode _enumEnvironmentMode;
        //private CancellationTokenSource _cts;
        //private bool _hasStockOrder = false;
        //private Dictionary<string, StockCandidate> _stockCandidateDict = new Dictionary<string, StockCandidate>();
        //private readonly IYuantaService _yuantaService;
        //private readonly ICandidateRepository _candidateRepository;
        //private readonly IDateTimeService _dateTimeService;
        //private readonly ILogger _logger;
        //private readonly string _stockAccount;
        //private readonly string _stockPassword;
        //private readonly string _todayDate;
        //private readonly DateTime _lastEntryTime;
        //private readonly DateTime _lastExitTime;
        //private readonly int _maxStockHoldingCount;
        //private readonly int _maxAmountPerStock;
        //private readonly decimal _profitPercentage;
        //public CrazyTraderService(IConfiguration config, IDateTimeService dateTimeService, IYuantaService yuantaService, ILogger logger, ICandidateRepository candidateRepository)
        //{
        //    string environment = config.GetValue<string>("Environment").ToUpper();
        //    _maxAmountPerStock = config.GetValue<int>("MaxAmountPerStock");
        //    _maxStockHoldingCount = config.GetValue<int>("MaxStockHoldingCount");
        //    _profitPercentage = config.GetValue<decimal>("ProfitPercentage");
        //    _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
        //    _dateTimeService = dateTimeService;
        //    DateTime now = _dateTimeService.GetTaiwanTime();
        //    DateTime programStopTime = now.Date.AddHours(13).AddMinutes(25);
        //    TimeSpan delayTime = programStopTime - now;
        //    _cts = delayTime > TimeSpan.Zero ? new CancellationTokenSource(delayTime) : new CancellationTokenSource();
        //    _todayDate = now.ToString("yyyy/MM/dd");
        //    _lastEntryTime = now.Date.AddHours(12).AddMinutes(30);
        //    _lastExitTime = now.Date.AddHours(13).AddMinutes(20);
        //    objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
        //    _stockAccount = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockAccount", EnvironmentVariableTarget.Machine) : "S98875005091";
        //    _stockPassword = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockPassword", EnvironmentVariableTarget.Machine) : "1234";
        //    _yuantaService = yuantaService;
        //    _logger = logger;
        //    _candidateRepository = candidateRepository;
        //}
        //public async Task Trade()
        //{
        //    try
        //    {
        //        _stockCandidateDict = (await _candidateRepository.GetActiveCrazyCandidate()).ToDictionary(x => x.StockCode);
        //        if (!_stockCandidateDict.Any()) return;
        //        objYuantaOneAPI.Open(_enumEnvironmentMode);
        //        await Task.Delay(-1, _cts.Token);
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        throw new Exception("Trade() 已被取消。");
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.ToString());
        //    }
        //    finally
        //    {
        //        objYuantaOneAPI.LogOut();
        //        objYuantaOneAPI.Close();
        //        objYuantaOneAPI.Dispose();
        //    }
        //}
        //void objApi_OnResponse(int intMark, uint dwIndex, string strIndex, object objHandle, object objValue)
        //{
        //    string strResult = "";
        //    try
        //    {
        //        switch (intMark)
        //        {
        //            case 0: //系統回應
        //                strResult = Convert.ToString(objValue);
        //                _yuantaService.SystemResponseHandler(strResult, objYuantaOneAPI, _stockAccount, _stockPassword, _cts, Subscribe);
        //                break;
        //            case 1: //代表為RQ/RP 所回應的
        //                switch (strIndex)
        //                {
        //                    case "Login":       //一般/子帳登入
        //                        strResult = _yuantaService.FunAPILogin_Out((byte[])objValue);
        //                        break;
        //                    case "30.100.10.31"://現貨下單
        //                        strResult = _yuantaService.FunStkOrder_Out((byte[])objValue);
        //                        StockOrderHandler(strResult);
        //                        break;
        //                    default:           //不在表列中的直接呈現訊息
        //                        strResult = $"{strIndex},{objValue}";
        //                        break;
        //                }
        //                break;
        //            case 2: //訂閱所回應
        //                switch (strIndex)
        //                {
        //                    case "200.10.10.26":    //逐筆即時回報
        //                        strResult = _yuantaService.FunRealReport_Out((byte[])objValue);
        //                        RealReportHandler(strResult);
        //                        break;
        //                    case "200.10.10.27":    //逐筆即時回報彙總
        //                        strResult = _yuantaService.FunRealReportMerge_Out((byte[])objValue);
        //                        break;
        //                    case "210.10.70.11":    //Watchlist報價表(指定欄位)
        //                        strResult = _yuantaService.FunRealWatchlist_Out((byte[])objValue);
        //                        WatchListHandler(strResult);
        //                        break;
        //                    case "210.10.60.10":    //訂閱五檔報價
        //                        strResult = _yuantaService.FunRealFivetick_Out((byte[])objValue);
        //                        FiveTickHandler(strResult, out string stockCode, out decimal level1AskPrice, out int level1AskSize);
        //                        StockOrder(stockCode, level1AskPrice, level1AskSize);
        //                        break;
        //                    default:
        //                        strResult = $"{strIndex},{objValue}";
        //                        break;
        //                }
        //                break;
        //            default:
        //                strResult = Convert.ToString(objValue);
        //                break;
        //        }
        //        _logger.Information(strResult);
        //    }
        //    catch (Exception ex)
        //    {
        //        strResult = "Error: " + ex;
        //        _logger.Error(strResult);
        //    }
        //}
        //private void FiveTickHandler(string strResult, out string stockCode, out decimal level1BidPrice, out int level1BidSize)
        //{
        //    stockCode = string.Empty;
        //    level1BidPrice = 0;
        //    level1BidSize = 0;
        //    if (string.IsNullOrEmpty(strResult)) return;
        //    string[] tickInfo = strResult.Split(',');
        //    stockCode = tickInfo[1];
        //    if (!decimal.TryParse(tickInfo[3], out level1BidPrice) || !int.TryParse(tickInfo[8], out level1BidSize))
        //    {
        //        _logger.Error($"The level1BidPrice or level1BidSize of Stock code {stockCode} is error");
        //        return;
        //    }
        //    level1BidPrice = level1BidPrice / 10000;
        //    _logger.Information($"Stock code: {stockCode}, Level 1 bid price: {level1BidPrice}, Level 1 bid size: {level1BidSize}");
        //}
        //private StockOrder SetDefaultStockOrder()
        //{
        //    StockOrder stockOrder = new StockOrder();
        //    stockOrder.Identify = Convert.ToInt32(001); // 識別碼
        //    stockOrder.Account = _stockAccount; // 現貨帳號
        //    stockOrder.APCode = Convert.ToInt16(0);    // 交易市場別, 0:一般 2:盤後零股 4:盤中零股 7:盤後
        //    stockOrder.TradeKind = Convert.ToInt16(0);  // 交易性質, 0:委託單 3:改量 4:取消 7:改價
        //    stockOrder.OrderType = "0";   // 委託種類, 0:現貨 3:融資 4:融券 5借券(賣出) 6:借券(賣出) 9:現股當沖
        //    stockOrder.TradeDate = _todayDate;
        //    stockOrder.SellerNo = Convert.ToInt16(0); // Convert.ToInt16(this.txtSellerNo.Text.Trim());    // 營業員代碼
        //    stockOrder.OrderNo = ""; // this.txtOrderNo.Text.Trim();   // 委託書編號
        //    stockOrder.BasketNo = ""; // this.txtBasketNo.Text.Trim(); // BasketNo
        //    return stockOrder;
        //}
        //private void StockOrder(string stockCode, decimal level1BidPrice, int level1BidSize)
        //{
        //    if (string.IsNullOrEmpty(stockCode) || level1BidPrice == 0 || level1BidSize == 0 || _hasStockOrder) return;
        //    if (!_stockCandidateDict.TryGetValue(stockCode, out StockCandidate candidate) || !candidate.IsTradingStarted) return;
        //    if (!candidate.IsHitProfitPoint && candidate.StopLossPoint / level1BidPrice > _profitPercentage)
        //    {
        //        candidate.IsHitProfitPoint = true;
        //    }
        //    if (candidate.IsHolding)
        //    {
        //        if (level1BidPrice >= candidate.EntryPoint || 
        //            candidate.StopLossPoint / level1BidPrice > _profitPercentage || 
        //            _dateTimeService.GetTaiwanTime() > _lastExitTime)
        //        {
        //            StockOrder stockOrder = SetDefaultStockOrder();
        //            stockOrder.StkCode = stockCode;
        //            stockOrder.PriceFlag = "M";
        //            stockOrder.BuySell = EBuySellType.B.ToString();
        //            stockOrder.Time_in_force = "0";
        //            stockOrder.Price = 0;
        //            stockOrder.OrderQty = candidate.PurchasedLot;
        //            ProcessStockOrder(stockOrder);
        //        }
        //    }
        //    else
        //    {
        //        int orderQty = (int)(_maxAmountPerStock / (candidate.EntryPoint * 1000));
        //        if (level1BidPrice == candidate.StopLossPoint &&
        //            !candidate.IsHitProfitPoint &&
        //            orderQty > 0 &&
        //            level1BidSize >= orderQty &&
        //            _maxStockHoldingCount > _stockCandidateDict.Count(x => x.Value.IsHolding) &&
        //            _dateTimeService.GetTaiwanTime() < _lastEntryTime)
        //        {
        //            StockOrder stockOrder = SetDefaultStockOrder();
        //            stockOrder.StkCode = stockCode;   // 股票代號
        //            stockOrder.PriceFlag = "";   // 價格種類, H:漲停 -:平盤  L:跌停 " ":限價  M:市價單
        //            stockOrder.BuySell = EBuySellType.S.ToString();   // 買賣別, B:買  S:賣
        //            stockOrder.Time_in_force = "4";  // 委託效期, 0:ROD 3:IOC  4:FOK
        //            stockOrder.Price = Convert.ToInt64(level1BidPrice * 10000);  // 委託價格
        //            stockOrder.OrderQty = Convert.ToInt64(orderQty);    // 委託單位數
        //            ProcessStockOrder(stockOrder);
        //        }
        //    }
        //}
        //private void ProcessStockOrder(StockOrder stockOrder)
        //{
        //    bool bResult = objYuantaOneAPI.SendStockOrder(_stockAccount, new List<StockOrder>() { stockOrder });
        //    if (bResult)
        //    {
        //        _hasStockOrder = true;
        //    }
        //    else
        //    {
        //        _logger.Error($"SendStockOrder error. Stock code: {stockOrder.StkCode}, Buy or Sell: {stockOrder.BuySell}");
        //    }
        //}
        //private void StockOrderHandler(string strResult)
        //{
        //    string[] resultArray = strResult.Split(',');
        //    if (string.IsNullOrEmpty(resultArray[4].Trim()) ||
        //        !DateTime.TryParse(resultArray[5], out DateTime orderTime) ||
        //        !string.IsNullOrEmpty(resultArray[resultArray.Length - 2].Trim()) ||
        //        !string.IsNullOrEmpty(resultArray[resultArray.Length - 1].Trim()))
        //    {
        //        _logger.Error($"SendStockOrder error. Error message: {resultArray[resultArray.Length - 2]}, {resultArray[resultArray.Length - 1]}");
        //        _hasStockOrder = false;
        //    }
        //}
        //private void RealReportHandler(string strResult)
        //{
        //    string[] reportArray = strResult.Split(',');
        //    if (!int.TryParse(reportArray[1].Split(':')[1], out int reportType))
        //    {
        //        _logger.Error("Report type error");
        //    }
        //    if (reportType != 51) return;
        //    string stockCode = reportArray[4].Trim();
        //    if (!_stockCandidateDict.TryGetValue(stockCode, out StockCandidate candidate)) return;
        //    if (!int.TryParse(reportArray[13], out int purchasedShare))
        //    {
        //        _logger.Error("PurchasedLot error");
        //    }
        //    if (reportArray[9] == EBuySellType.S.ToString())
        //    {
        //        candidate.IsHolding = true;
        //        candidate.PurchasedLot = purchasedShare / 1000;
        //    }
        //    else
        //    {
        //        candidate.IsHolding = false;
        //        candidate.PurchasedLot = 0;
        //    }
        //    _hasStockOrder = false;
        //}
        //private void WatchListHandler(string strResult)
        //{
        //    string[] watchListResult = strResult.Split(',');
        //    string stockCode = watchListResult[1];
        //    if (!decimal.TryParse(watchListResult[3], out decimal tradePrice)) return;
        //    if (!_stockCandidateDict.TryGetValue(stockCode, out StockCandidate candidate)) return;
        //    candidate.IsTradingStarted = true;
        //    UnsubscribeWatchlist(candidate.Market, candidate.StockCode);
        //}
        //private void UnsubscribeWatchlist(enumMarketType enumMarketNo, string stockCode)
        //{
        //    Watchlist watch = new Watchlist();
        //    watch.IndexFlag = Convert.ToByte(7);    //填入定閱索引值, 7: 成交價
        //    watch.MarketNo = Convert.ToByte(enumMarketNo);      //填入查詢市場代碼
        //    watch.StockCode = stockCode;                     //填入查詢股票代碼
        //    objYuantaOneAPI.UnsubscribeWatchlist(new List<Watchlist>() { watch });
        //}
        //private void Subscribe()
        //{
        //    List<FiveTickA> lstFiveTick = new List<FiveTickA>();
        //    List<Watchlist> lstWatchlist = new List<Watchlist>();
        //    foreach (var i in _stockCandidateDict)
        //    {
        //        FiveTickA fiveTickA = new FiveTickA();
        //        fiveTickA.MarketNo = Convert.ToByte(i.Value.Market);
        //        fiveTickA.StockCode = i.Value.StockCode;
        //        lstFiveTick.Add(fiveTickA);
        //        Watchlist watch = new Watchlist();
        //        watch.IndexFlag = Convert.ToByte(7);    //填入訂閱索引值, 7: 成交價
        //        watch.MarketNo = Convert.ToByte(i.Value.Market);      //填入查詢市場代碼
        //        watch.StockCode = i.Value.StockCode;
        //        lstWatchlist.Add(watch);
        //    }
        //    lstFiveTick = lstFiveTick.GroupBy(x => x.StockCode).Select(g => g.First()).ToList();
        //    lstWatchlist = lstWatchlist.GroupBy(x => x.StockCode).Select(g => g.First()).ToList();
        //    objYuantaOneAPI.SubscribeFiveTickA(lstFiveTick);
        //    objYuantaOneAPI.SubscribeWatchlist(lstWatchlist);
        //}
    }
}
