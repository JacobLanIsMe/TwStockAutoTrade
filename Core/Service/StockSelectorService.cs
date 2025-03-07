using Core.Enum;
using Core.HttpClientFactory;
using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Service
{
    public class StockSelectorService : IStockSelectorService
    {
        private HttpClient _httpClient;
        private readonly ICandidateRepository _candidateRepository;
        private readonly ITradeRepository _tradeRepository;
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger _logger;
        public StockSelectorService(ICandidateRepository candidateRepository, ITradeRepository tradeRepository, ILogger logger)
        {
            SimpleHttpClientFactory httpClientFactory = new SimpleHttpClientFactory();
            _httpClient = httpClientFactory.CreateClient();
            _candidateRepository = candidateRepository;
            _tradeRepository = tradeRepository;
            int maxConcurrency = Environment.ProcessorCount * 40;
            _semaphore = new SemaphoreSlim(maxConcurrency);
            _logger = logger;
        }
        public async Task SelectStock()
        {
            var twseStockListTask = GetTwseStockCode();
            var tpexStockListTask = GetTwotcStockCode();
            var twseStockList = await twseStockListTask;
            var tpexStockList = await tpexStockListTask;
            List<Candidate> allStockInfoList = twseStockList.Concat(tpexStockList).ToList();
            await GetDailyExchangeReport(allStockInfoList);
            List<Candidate> candidateList = SelectCandidate(allStockInfoList);
            Dictionary<string, Candidate> allStockInfoDict = allStockInfoList.ToDictionary(x => x.StockCode);
            await UpdateCandidate(candidateList, allStockInfoDict);
            await UpdateTrade(allStockInfoDict);
        }
        private async Task<List<Candidate>> GetTwseStockCode()
        {
            _logger.Information("Get TWSE stock code started.");
            HttpResponseMessage response = await _httpClient.GetAsync("https://openapi.twse.com.tw/v1/opendata/t187ap03_L");
            string responseBody = await response.Content.ReadAsStringAsync();
            List<TwseStockInfo> stockInfoList = JsonConvert.DeserializeObject<List<TwseStockInfo>>(responseBody);
            HttpResponseMessage intradayResponse = await _httpClient.GetAsync("https://openapi.twse.com.tw/v1/exchangeReport/TWTB4U");
            string intradayResponseBody = await intradayResponse.Content.ReadAsStringAsync();
            List<TwseIntradayStockInfo> intradayStockInfoList = JsonConvert.DeserializeObject<List<TwseIntradayStockInfo>>(intradayResponseBody);
            HashSet<string> intradayStockCodeHashSet = new HashSet<string>(intradayStockInfoList.Select(x => x.Code.ToUpper()));
            List<Candidate> stockList = stockInfoList.Where(x => intradayStockCodeHashSet.Contains(x.公司代號.ToUpper())).Select(x => new Candidate()
            {
                Market = EMarket.TWSE,
                StockCode = x.公司代號.ToUpper(),
                CompanyName = x.公司簡稱
            }).ToList();
            _logger.Information("Get TWSE stock code finished.");
            return stockList;
        }
        private async Task<List<Candidate>> GetTwotcStockCode()
        {
            _logger.Information("Get TWOTC stock code started.");
            HttpResponseMessage response = await _httpClient.GetAsync("https://www.tpex.org.tw/openapi/v1/mopsfin_t187ap03_O");
            string responseBody = await response.Content.ReadAsStringAsync();
            List<TwotcStockInfo> stockInfoList = JsonConvert.DeserializeObject<List<TwotcStockInfo>>(responseBody);
            HttpResponseMessage intradayResponse = await _httpClient.GetAsync("https://www.tpex.org.tw/www/zh-tw/intraday/list");
            string intradayResponseBody = await intradayResponse.Content.ReadAsStringAsync();
            TwotcIntradayStockInfo intradayStockInfoList = JsonConvert.DeserializeObject<TwotcIntradayStockInfo>(intradayResponseBody);
            HashSet<string> intradayStockCodeHashSet = new HashSet<string>();
            if (intradayStockInfoList.Tables.Any())
            {
                intradayStockCodeHashSet = new HashSet<string>(intradayStockInfoList.Tables.First().Data.Select(x => x[0].ToUpper()));
            }
            List<Candidate> stockList = stockInfoList.Where(x => intradayStockCodeHashSet.Contains(x.SecuritiesCompanyCode.ToUpper())).Select(x => new Candidate()
            {
                Market = EMarket.TWOTC,
                StockCode = x.SecuritiesCompanyCode.ToUpper(),
                CompanyName = x.CompanyAbbreviation
            }).ToList();
            _logger.Information("Get TWOTC stock code finished.");
            return stockList;
        }
        private async Task GetDailyExchangeReport(List<Candidate> stockList)
        {
            _logger.Information("Retrieve exchange report started.");
            var tasks = stockList.Select(async stock =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    string url = $"https://tw.quote.finance.yahoo.net/quote/q?type=ta&perd=d&mkt=10&sym={stock.StockCode}&v=1&callback=jQuery111306311117094962886_1574862886629&_=1574862886630";
                    HttpResponseMessage response = await _httpClient.GetAsync(url);
                    string responseBody = await response.Content.ReadAsStringAsync();
                    string modifiedResponseBody = responseBody.Split(new string[] { "\"ta\":" }, StringSplitOptions.None)[1];
                    modifiedResponseBody = modifiedResponseBody.Split(new string[] { ",\"ex\":" }, StringSplitOptions.None)[0];
                    modifiedResponseBody = modifiedResponseBody.TrimEnd(new char[] { '}', ')', ';' });
                    List<YahooTechData> yahooTechData = JsonConvert.DeserializeObject<List<YahooTechData>>(modifiedResponseBody);
                    stock.TechDataList = yahooTechData.Select(x => new StockTechData
                    {
                        Date = DateTime.ParseExact(x.T.ToString(), "yyyyMMdd", null),
                        Close = x.C,
                        Open = x.O,
                        High = x.H,
                        Low = x.L,
                        Volume = x.V
                    }).OrderByDescending(x => x.Date).ToList();
                    _logger.Information($"Retrieve exchange report of Stock {stock.StockCode} {stock.CompanyName} finished.");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error occurs while retrieving exchange report of Stock {stock.StockCode} {stock.CompanyName}. Error message: {ex}");
                }
                finally
                {
                    _semaphore.Release();
                }
            });
            await Task.WhenAll(tasks);
            _logger.Information("Retrieve exchange report finished.");
        }
        private List<Candidate> SelectCandidate(List<Candidate> stockList)
        {
            List<Candidate> candidateList = new List<Candidate>();
            foreach (var i in stockList)
            {
                i.IsCandidate = IsCandidate(i.TechDataList, out StockTechData gapUpTechData);
                if (!i.IsCandidate || gapUpTechData == null) continue;
                i.GapUpHigh = gapUpTechData.High;
                i.GapUpLow = gapUpTechData.Low;
                i.SelectedDate = i.TechDataList.First().Date;
                i.StopLossPoint = GetStopLossPoint((decimal)i.GapUpHigh);
                i.Last9TechData = JsonConvert.SerializeObject(i.TechDataList.Take(9));
                candidateList.Add(i);
            }
            return candidateList;
        }
        private bool IsCandidate(List<StockTechData> techDataList, out StockTechData gapUpTechData)
        {
            gapUpTechData = null;
            if (techDataList.Count < 60) return false;
            gapUpTechData = techDataList[4];
            if (gapUpTechData.Low <= techDataList[5].High) return false;
            decimal ma60 = techDataList.Take(60).Average(x => x.Close);
            if (gapUpTechData.High / ma60 > (decimal)1.1) return false;
            double mv5 = techDataList.Take(5).Average(x => x.Volume);
            if (mv5 < 100) return false;
            decimal volatility = techDataList.Take(5).Max(x => x.Close) / techDataList.Take(5).Min(x => x.Close);
            if (volatility > (decimal)1.02) return false;
            decimal gapUpMa5 = techDataList.Skip(4).Take(5).Average(x => x.Close);
            decimal gapUpMa10 = techDataList.Skip(4).Take(10).Average(x => x.Close);
            decimal gapUpMa20 = techDataList.Skip(4).Take(20).Average(x => x.Close);
            if (gapUpTechData.Close < gapUpMa5 || gapUpTechData.Close < gapUpMa10 || gapUpTechData.Close < gapUpMa20) return false;
            List<decimal> last4Close = techDataList.Take(4).Select(x => x.Close).ToList();
            bool isPeriodCloseHigherThanGapUpHigh = last4Close.Max() > gapUpTechData.High;
            bool isPeriodCloseLowerThanGapUpLow = last4Close.Min() < gapUpTechData.Low;
            if (isPeriodCloseHigherThanGapUpHigh || isPeriodCloseLowerThanGapUpLow) return false;
            return true;
        }
        private decimal GetStopLossPoint(decimal gapUpHigh)
        {
            if (gapUpHigh <= 10)
            {
                return gapUpHigh - (decimal)(0.01 * 2);
            }
            else if (gapUpHigh == (decimal)10.05)
            {
                return (decimal)9.99;
            }
            else if (gapUpHigh > (decimal)10.05 & gapUpHigh <= 50)
            {
                return gapUpHigh - (decimal)(0.05 * 2);
            }
            else if (gapUpHigh == (decimal)50.1)
            {
                return (decimal)49.95;
            }
            else if (gapUpHigh > (decimal)50.1 & gapUpHigh <= 100)
            {
                return gapUpHigh - (decimal)(0.1 * 2);
            }
            else if (gapUpHigh == (decimal)100.5)
            {
                return (decimal)99.9;
            }
            else if (gapUpHigh > (decimal)100.5 & gapUpHigh <= 500)
            {
                return gapUpHigh - (decimal)(0.5 * 2);
            }
            else if (gapUpHigh == (decimal)501)
            {
                return (decimal)499.5;
            }
            else if (gapUpHigh > 501 & gapUpHigh <= 1000)
            {
                return gapUpHigh - (1 * 2);
            }
            else if (gapUpHigh == 1005)
            {
                return 999;
            }
            else
            {
                return gapUpHigh - (5 * 2);
            }
        }
        private async Task UpdateCandidate(List<Candidate> candidateToInsertList, Dictionary<string, Candidate> allStockInfoDict)
        {
            List<Candidate> activeCandidateList = await _candidateRepository.GetActiveCandidate();
            Dictionary<string, Candidate> candidateDict = candidateToInsertList.ToDictionary(x => x.StockCode);
            List<Guid> candidateToDeleteList = new List<Guid>();
            List<Candidate> candidateToUpdateList = new List<Candidate>();
            foreach (var i in activeCandidateList)
            {
                if (candidateDict.ContainsKey(i.StockCode))
                {
                    candidateToDeleteList.Add(i.Id);
                    continue;
                }
                if (!allStockInfoDict.TryGetValue(i.StockCode, out Candidate stock))
                {
                    candidateToDeleteList.Add(i.Id);
                    continue;
                }
                if (stock.TechDataList.Count <= 0)
                {
                    candidateToDeleteList.Add(i.Id);
                    continue;
                }
                decimal todayClose = stock.TechDataList.First().Close;
                if (todayClose < i.GapUpLow)
                {
                    candidateToDeleteList.Add(i.Id);
                    continue;
                }
                if (todayClose >= i.GapUpHigh)
                {
                    if (stock.TechDataList.Count < 10)
                    {
                        candidateToDeleteList.Add(i.Id);
                        continue;
                    }
                    decimal ma10 = stock.TechDataList.Take(10).Average(x => x.Close);
                    if (todayClose < ma10)
                    {
                        candidateToDeleteList.Add(i.Id);
                        continue;
                    }
                }
                i.Last9TechData = JsonConvert.SerializeObject(stock.TechDataList.Take(9));
                candidateToUpdateList.Add(i);
            }
            await _candidateRepository.Update(candidateToDeleteList, candidateToUpdateList, candidateToInsertList);
        }
        public async Task UpdateTrade(Dictionary<string, Candidate> allStockInfoDict)
        {
            List<Trade> stockHoldingList = await _tradeRepository.GetStockHolding();
            foreach (var i in stockHoldingList)
            {
                if (!allStockInfoDict.TryGetValue(i.StockCode, out Candidate stock) || stock.TechDataList.Count < 9)
                {
                    _logger.Error($"Can not retrieve last 9 tech data of stock code {i.StockCode}.");
                    continue;
                }
                i.Last9TechData = JsonConvert.SerializeObject(stock.TechDataList.Take(9));
            }
            await _tradeRepository.UpdateLast9TechData(stockHoldingList);
        }
    }
}