﻿using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Service
{
    public class StockTraderService : IStockTraderService
    {
        YuantaOneAPITrader objYuantaOneAPI = new YuantaOneAPITrader();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Dictionary<string, StockTrade> _stockHoldingDict = new Dictionary<string, StockTrade>();
        private Dictionary<string, StockCandidate> _stockCandidateDict = new Dictionary<string, StockCandidate>();

        private readonly StockTradeConfig tradeConfig;
        private readonly ITradeRepository _tradeRepository;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger _logger;
        private readonly enumEnvironmentMode _enumEnvironmentMode;
        private readonly IYuantaService _yuantaService;
        private readonly string _stockAccount;
        private readonly string _stockPassword;
        public StockTraderService(IConfiguration config, ITradeRepository tradeRepository, ICandidateRepository candidateRepository, IDateTimeService dateTimeService, ILogger logger, IYuantaService yuantaService)
        {
            tradeConfig = config.GetSection("TradeConfig").Get<StockTradeConfig>();
            string environment = config.GetValue<string>("Environment").ToUpper();
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _tradeRepository = tradeRepository;
            _candidateRepository = candidateRepository;
            _dateTimeService = dateTimeService;
            _logger = logger;
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
            _yuantaService = yuantaService;
            _stockAccount = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockAccount") : "S98875005091";
            _stockPassword = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("StockPassword") : "1234";
        }
        public async Task Trade()
        {
            try
            {
                _stockHoldingDict = (await _tradeRepository.GetStockHolding()).ToDictionary(x => x.StockCode);
                _stockCandidateDict = (await _candidateRepository.GetActiveCandidate()).ToDictionary(x=>x.StockCode);
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
                        _yuantaService.SystemResponseHandler(strResult, objYuantaOneAPI, _stockAccount, _stockPassword, _cts, SubscribeStockTick);
                        break;
                    case 1: //代表為RQ/RP 所回應的
                        switch (strIndex)
                        {
                            case "Login":       //一般/子帳登入
                                strResult = _yuantaService.FunAPILogin_Out((byte[])objValue);
                                break;
                            default:           //不在表列中的直接呈現訊息
                                strResult = $"{strIndex},{objValue}";
                                break;
                        }
                        break;
                    case 2: //訂閱所回應
                        switch (strIndex)
                        {
                            case "210.10.70.11":    //Watchlist報價表(指定欄位)
                                strResult = _yuantaService.FunRealWatchlist_Out((byte[])objValue);
                                TickHandler(strResult, out string stockCode, out decimal tickPrice);
                                StockOrder(stockCode, tickPrice);
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
                _logger.Information(strResult);
            }
            catch (Exception ex)
            {
                strResult = "Error: " + ex;
                _logger.Error(strResult);
            }
        }
        private void TickHandler(string strResult, out string stockCode, out decimal tickPrice)
        {
            stockCode = "";
            tickPrice = 0;
            if (string.IsNullOrEmpty(strResult)) return;
            string[] tickInfo = strResult.Split(',');
            stockCode = tickInfo[1];
            if (!decimal.TryParse(tickInfo[3], out tickPrice))
            {
                _logger.Error($"The price of Stock code {stockCode} is error");
                return;
            }
            tickPrice = tickPrice / 10000;
        }
        private void StockOrder(string stockCode, decimal tickPrice)
        {
            
            bool hasStockHolding = _stockHoldingDict.Values.Select(x => x.SaleDate == null).Count() > 0;
            if (hasStockHolding)
            {
                
            }
            else
            {

            }
        }
        private void SubscribeStockTick()
        {
            List<Watchlist> lstWatchlist = new List<Watchlist>();
            lstWatchlist.AddRange(_stockHoldingDict.Select(x => new Watchlist
            {
                IndexFlag = Convert.ToByte(7),  // 訂閱索引值, 7: 成交價
                MarketNo = Convert.ToByte(x.Value.Market),
                StockCode = x.Value.StockCode,
            }));
            lstWatchlist.AddRange(_stockCandidateDict.Select(x => new Watchlist
            {
                IndexFlag = Convert.ToByte(7),  // 訂閱索引值, 7: 成交價
                MarketNo = Convert.ToByte(x.Value.Market),
                StockCode = x.Value.StockCode
            }));
            lstWatchlist = lstWatchlist.GroupBy(x => x.StockCode).Select(g => g.First()).ToList();
            objYuantaOneAPI.SubscribeWatchlist(lstWatchlist);
        }
    }
}
