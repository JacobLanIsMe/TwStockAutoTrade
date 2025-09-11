using Core.Enum;
using Core.Model;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Service
{
    public class ShortStockService : IShortStockService
    {
        YuantaOneAPITrader objYuantaOneAPI = new YuantaOneAPITrader();
        private readonly enumEnvironmentMode _enumEnvironmentMode;
        private readonly string _stockAccount;
        private readonly string _stockPassword;
        private readonly ILogger _logger;
        private readonly IYuantaService _yuantaService;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public ShortStockService(IConfiguration config, ILogger logger, IYuantaService yuantaService) 
        {
            string environment = config.GetValue<string>("Environment").ToUpper();
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _stockAccount = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockAccount", EnvironmentVariableTarget.Machine) : "S98875005091";
            _stockPassword = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockPassword", EnvironmentVariableTarget.Machine) : "1234";
            _logger = logger;
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
            _yuantaService = yuantaService;
        }
        public async Task Trade()
        {
            try
            {
                objYuantaOneAPI.Open(_enumEnvironmentMode);
                await Task.Delay(-1, _cts.Token);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Trade() 已被取消。");
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                objYuantaOneAPI.LogOut();
                objYuantaOneAPI.Close();
                objYuantaOneAPI.Dispose();
            }
        }
        void objApi_OnResponse(int intMark, uint dwIndex, string strIndex, object objHandle, object objValue)
        {
            string strResult = "";
            try
            {
                switch (intMark)
                {
                    case 0: //系統回應
                        strResult = Convert.ToString(objValue);
                        _yuantaService.SystemResponseHandler(strResult, objYuantaOneAPI, _stockAccount, _stockPassword, _cts, Subscribe);
                        break;
                    case 1: //代表為RQ/RP 所回應的
                        switch (strIndex)
                        {
                            case "Login":       //一般/子帳登入
                                strResult = _yuantaService.FunAPILogin_Out((byte[])objValue);
                                break;
                            case "30.100.10.31"://現貨下單
                                strResult = _yuantaService.FunStkOrder_Out((byte[])objValue);
                                StockOrderHandler(strResult);
                                break;
                            default:           //不在表列中的直接呈現訊息
                                strResult = $"{strIndex},{objValue}";
                                break;
                        }
                        break;
                    case 2: //訂閱所回應
                        switch (strIndex)
                        {
                            case "200.10.10.26":    //逐筆即時回報
                                strResult = _yuantaService.FunRealReport_Out((byte[])objValue);
                                RealReportHandler(strResult);
                                break;
                            case "210.10.70.11":    //Watchlist報價表(指定欄位)
                                strResult = _yuantaService.FunRealWatchlist_Out((byte[])objValue);
                                WatchListHandler(strResult);
                                break;
                            case "210.10.60.10":    //訂閱五檔報價
                                string fivetickResult = _yuantaService.FunRealFivetick_Out((byte[])objValue);
                                FiveTickHandler(fivetickResult, out string stockCode, out decimal level1AskPrice, out int level1AskSize);
                                StockOrder(stockCode, level1AskPrice, level1AskSize);
                                break;
                            default:
                                strResult = $"{strIndex},{objValue}";
                                break;
                        }
                        break;
                    default:
                        strResult = Convert.ToString(objValue);
                        break;
                }
                if (!string.IsNullOrEmpty(strResult))
                {
                    _logger.Information(strResult);
                }
            }
            catch (Exception ex)
            {
                strResult = "Error: " + ex;
                _logger.Error(strResult);
            }
        }
        private void StockOrder(string stockCode, decimal level1AskPrice, int level1AskSize)
        {
            if (string.IsNullOrEmpty(stockCode) || level1AskPrice == 0 || level1AskSize == 0 || _trade != null) return;
            if (!_stockCandidateDict.TryGetValue(stockCode, out StockCandidate candidate) || !candidate.IsTradingStarted) return;
            if (candidate.PurchasedLot > 0)
            {
                if ((level1AskPrice / candidate.EntryPoint >= 1.1m) ||
                    (level1AskPrice < candidate.StopLossPoint))
                {
                    StockOrder stockOrder = SetDefaultStockOrder();
                    stockOrder.StkCode = stockCode;
                    stockOrder.PriceFlag = "M";
                    stockOrder.BuySell = EBuySellType.S.ToString();
                    stockOrder.Time_in_force = "0";
                    stockOrder.Price = Convert.ToInt64(0);
                    stockOrder.OrderQty = candidate.PurchasedLot;
                    ProcessStockOrder(stockOrder);
                }
            }
            else
            {
                if (candidate.ExRrightsExDividendDateTime.HasValue && _now.AddDays(8).Date > candidate.ExRrightsExDividendDateTime.Value.Date) return;
                int orderQty = (int)(tradeConfig.MaxAmountPerStock / (candidate.EntryPoint * 1000));
                if (level1AskPrice == candidate.EntryPoint &&
                    orderQty > 0 &&
                    level1AskSize >= orderQty &&
                    tradeConfig.MaxStockCount > _stockCandidateDict.Count(x => x.Value.PurchasedLot > 0))
                {
                    StockOrder stockOrder = SetDefaultStockOrder();
                    stockOrder.StkCode = stockCode;   // 股票代號
                    stockOrder.PriceFlag = "";   // 價格種類, H:漲停 -:平盤  L:跌停 " ":限價  M:市價單
                    stockOrder.BuySell = EBuySellType.B.ToString();   // 買賣別, B:買  S:賣
                    stockOrder.Time_in_force = "4";  // 委託效期, 0:ROD 3:IOC  4:FOK
                    stockOrder.Price = Convert.ToInt64(level1AskPrice * 10000);  // 委託價格
                    stockOrder.OrderQty = Convert.ToInt64(orderQty);    // 委託單位數
                    ProcessStockOrder(stockOrder);
                }
            }
        }
        private void StockOrderHandler(string strResult)
        {
            string[] resultArray = strResult.Split(',');
            if (string.IsNullOrEmpty(resultArray[4].Trim()) ||
                !DateTime.TryParse(resultArray[5], out DateTime orderTime) ||
                !string.IsNullOrEmpty(resultArray[resultArray.Length - 2].Trim()) ||
                !string.IsNullOrEmpty(resultArray[resultArray.Length - 1].Trim()))
            {
                _logger.Error($"SendStockOrder error. Error message: {resultArray[resultArray.Length - 2]}, {resultArray[resultArray.Length - 1]}");
                _trade = null;
            }
        }
        private void RealReportHandler(string strResult)
        {
            string[] reportArray = strResult.Split(',');
            if (!int.TryParse(reportArray[1].Split(':')[1].Trim(), out int reportType))
            {
                _logger.Error("Report type error");
            }
            if (!int.TryParse(reportArray[13].Trim(), out int purchasedShare))
            {
                _logger.Error("PurchasedLot error");
            }
            purchasedShare = purchasedShare / 1000;
            string orderNo = reportArray[2].Trim().Substring(4); // 委託單號
            string stockCode = reportArray[4].Trim();
            if (reportType == 50)
            {
                string errorCode = reportArray[reportArray.Length - 1].Trim();
                if (errorCode == "13048" || errorCode == "19348")
                {
                    _trade = null;
                    _logger.Warning($"委託失效，FOK 委託未能成功, Stock code: {stockCode}");
                }
            }
            else if (reportType == 51)
            {
                if (stockCode != _trade.StockCode) return;
                if (!_stockCandidateDict.TryGetValue(stockCode, out StockCandidate candidate)) return;
                if (reportArray[9].Trim() == EBuySellType.B.ToString())
                {
                    candidate.PurchasedLot = candidate.PurchasedLot + purchasedShare;
                    if (candidate.PurchasedLot == _trade.OrderedLot)
                    {
                        _trade = null;
                    }
                }
                else
                {
                    candidate.PurchasedLot = candidate.PurchasedLot - purchasedShare;
                    if (candidate.PurchasedLot == 0)
                    {
                        _trade = null;
                    }
                }
            }
        }
        private void WatchListHandler(string strResult)
        {
            string[] watchListResult = strResult.Split(',');
            string stockCode = watchListResult[1];
            if (!decimal.TryParse(watchListResult[3], out decimal tradePrice)) return;
            if (!_stockCandidateDict.TryGetValue(stockCode, out StockCandidate candidate)) return;
            candidate.IsTradingStarted = true;
            UnsubscribeWatchlist(candidate.Market, candidate.StockCode);
        }
        private void FiveTickHandler(string strResult, out string stockCode, out decimal level1AskPrice, out int level1AskSize)
        {
            stockCode = "";
            level1AskPrice = 0;
            level1AskSize = 0;
            if (string.IsNullOrEmpty(strResult)) return;
            string[] tickInfo = strResult.Split(',');
            stockCode = tickInfo[1];
            if (!decimal.TryParse(tickInfo[13], out level1AskPrice) || !int.TryParse(tickInfo[18], out level1AskSize))
            {
                _logger.Error($"The level1AskPrice or level1AskSize of Stock code {stockCode} is error");
                return;
            }
            level1AskPrice = level1AskPrice / 10000;
        }
        private void Subscribe()
        {
            List<FiveTickA> lstFiveTick = new List<FiveTickA>();
            List<Watchlist> lstWatchlist = new List<Watchlist>();
            foreach (var i in _stockCandidateDict)
            {
                FiveTickA fiveTickA = new FiveTickA();
                fiveTickA.MarketNo = Convert.ToByte(i.Value.Market);
                fiveTickA.StockCode = i.Value.StockCode;
                lstFiveTick.Add(fiveTickA);
                Watchlist watch = new Watchlist();
                watch.IndexFlag = Convert.ToByte(7);    //填入訂閱索引值, 7: 成交價
                watch.MarketNo = Convert.ToByte(i.Value.Market);      //填入查詢市場代碼
                watch.StockCode = i.Value.StockCode;
                lstWatchlist.Add(watch);
            }
            objYuantaOneAPI.SubscribeFiveTickA(lstFiveTick);
            objYuantaOneAPI.SubscribeWatchlist(lstWatchlist);
        }
    }
}
