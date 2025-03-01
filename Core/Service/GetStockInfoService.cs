using Core.Enum;
using Core.HttpClientFactory;
using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Service
{
    public class GetStockInfoService : IGetStockInfoService
    {
        private HttpClient _httpClient;
        private readonly ICandidateRepository _candidateRepository;
        private readonly SemaphoreSlim _semaphore;
        public GetStockInfoService(ICandidateRepository candidateRepository)
        {
            SimpleHttpClientFactory httpClientFactory = new SimpleHttpClientFactory();
            _httpClient = httpClientFactory.CreateClient();
            _candidateRepository = candidateRepository;
            int maxConcurrency = Environment.ProcessorCount * 40;
            _semaphore = new SemaphoreSlim(maxConcurrency);
        }
        public async Task SelectStock()
        {
            var twseStockListTask = GetTwseStockCode();
            var tpexStockListTask = GetTwotcStockCode();
            var twseStockList = await twseStockListTask;
            var tpexStockList = await tpexStockListTask;
            List<Candidate> mergedStockList = twseStockList.Concat(tpexStockList).ToList();
            await GetDailyExchangeReport(mergedStockList);
            List<Candidate> candidateList = SelectCandidate(mergedStockList); ;


            await _candidateRepository.Insert(candidateList);
            await DeleteActiveCandidate(mergedStockList);
        }
        private async Task<List<Candidate>> GetTwseStockCode()
        {
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
            return stockList;
        }
        private async Task<List<Candidate>> GetTwotcStockCode()
        {
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
            return stockList;
        }
        private async Task GetDailyExchangeReport(List<Candidate> stockList)
        {
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
                    Console.WriteLine($"Retrieve exchange report of Stock {stock.StockCode} {stock.CompanyName} finished.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurs while retrieving exchange report of Stock {stock.StockCode} {stock.CompanyName}. Error message: {ex}");
                }
                finally
                {
                    _semaphore.Release();
                }
            });
            await Task.WhenAll(tasks);
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
                i.SelectDate = i.TechDataList.First().Date;
                i.StopLossPoint = GetStopLossPoint((decimal)i.GapUpHigh);
                candidateList.Add(i);
            }
            return candidateList;
        }
        private bool IsCandidate(List<StockTechData> techDataList, out StockTechData gapUpTechData)
        {
            gapUpTechData = null;
            if (techDataList.Count < 25) return false;
            gapUpTechData = techDataList[4];
            if (gapUpTechData.Low <= techDataList[5].High) return false;
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
        private async Task DeleteActiveCandidate(List<Candidate> candidateList)
        {
            Dictionary<string, Candidate> candidateDict = candidateList.ToDictionary(x => x.StockCode);
            List<Candidate> activeCandidateList = await _candidateRepository.GetActiveCandidate();
            if (!activeCandidateList.Any()) return;
            List<Guid> candidateToDeleteList = new List<Guid>();
            List<Guid> duplicateActiveCandidate = activeCandidateList.GroupBy(x => x.StockCode).SelectMany(g => g.OrderByDescending(x => x.SelectDate).Skip(1)).Select(x => x.Id).ToList();
            candidateToDeleteList.AddRange(duplicateActiveCandidate);
            activeCandidateList = activeCandidateList.Where(x => !candidateToDeleteList.Contains(x.Id)).ToList();
            foreach (var i in activeCandidateList)
            {
                if (candidateDict.TryGetValue(i.StockCode, out Candidate stock))
                {
                    if (stock.TechDataList.Count < 10)
                    {
                        candidateToDeleteList.Add(i.Id);
                    }
                    else
                    {
                        List<StockTechData> techDataList = stock.TechDataList;
                        if (techDataList.First().Close < techDataList.Take(10).Average(x => x.Close) &
                            techDataList.First().Close < i.GapUpLow)
                        {
                            candidateToDeleteList.Add(i.Id);
                        }
                    }
                }
                else
                {
                    candidateToDeleteList.Add(i.Id);
                }
            }
            await _candidateRepository.UpdateIsDeleteById(candidateToDeleteList);
        }
        //private async Task GetTwseDailyExchangeRecort(List<Candidate> twseStockList)
        //{
        //    DateTime now = _dateTimeService.GetTaiwanTime();
        //    List<string> monthList = new List<string>();
        //    for (int i = 0; i < 3; i++)
        //    {
        //        monthList.Add(now.AddMonths(i * -1).ToString("yyyyMM") + "01");
        //    }
        //    foreach (var stock in twseStockList)
        //    {
        //        try
        //        {
        //            foreach (var month in monthList)
        //            {
        //                string url = $"https://www.twse.com.tw/rwd/zh/afterTrading/STOCK_DAY?date={month}&stockNo={stock.StockCode}&response=json";
        //                HttpResponseMessage response = await _httpClient.GetAsync(url);
        //                string responseBody = await response.Content.ReadAsStringAsync();
        //                TwseStockExchangeReport data = JsonConvert.DeserializeObject<TwseStockExchangeReport>(responseBody);
        //                foreach (var i in data.Data)
        //                {
        //                    StockTechData techData = new StockTechData()
        //                    {
        //                        Date = DateTime.ParseExact((int.Parse(i[0].Replace("/", "")) + 19110000).ToString(), "yyyyMMdd", null),
        //                        Volume = int.Parse(i[1].Replace(",", "")),
        //                        Open = decimal.Parse(i[3]),
        //                        High = decimal.Parse(i[4]),
        //                        Low = decimal.Parse(i[5]),
        //                        Close = decimal.Parse(i[6]),
        //                    };
        //                    stock.TechDataList.Add(techData);
        //                }
        //            }
        //            Console.WriteLine($"Retrieve exchange report of Stock {stock.StockCode} {stock.CompanyName} finished.");
        //            await Task.Delay(5000);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error occurs while retrieving exchange report of Stock {stock.StockCode} {stock.CompanyName}. Error message: {ex}");
        //        }
        //    }
        //}

    }
}