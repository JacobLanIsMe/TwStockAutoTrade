using Core.Enum;
using Core.HttpClientFactory;
using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Core.Service
{
    public class GetStockInfoService : IGetStockInfoService
    {
        private HttpClient _httpClient;
        private readonly IDateTimeService _dateTimeService;
        private readonly ICandidateRepository _candidateRepository;
        public GetStockInfoService(IDateTimeService dateTimeService, ICandidateRepository candidateRepository)
        {
            SimpleHttpClientFactory httpClientFactory = new SimpleHttpClientFactory();
            _httpClient = httpClientFactory.CreateClient();
            _dateTimeService = dateTimeService;
            _candidateRepository = candidateRepository;
        }
        public async Task SelectStock()
        {
            var twseStockListTask = GetTwseStockCode();
            var tpexStockListTask = GetTwotcStockCode();
            var twseStockList = await twseStockListTask;
            var tpexStockList = await tpexStockListTask;
            //await GetTwseDailyExchangeRecort(twseStockList);
            List<Candidate> mergedStockList = twseStockList.Concat(tpexStockList).ToList();
            List<Candidate> candidateList = mergedStockList.Where(x => x.IsCandidate).ToList();
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
        private async Task GetTwseDailyExchangeRecort(List<Candidate> twseStockList)
        {
            DateTime now = _dateTimeService.GetTaiwanTime();
            string thisMonth = now.ToString("yyyyMM") + "01";
            string prevMonth = now.AddMonths(-1).ToString("yyyyMM") + "01";
            List<string> monthList = new List<string>() { thisMonth, prevMonth };
            foreach (var stock in twseStockList)
            {
                try
                {
                    foreach (var month in monthList)
                    {
                        string url = $"https://www.twse.com.tw/rwd/zh/afterTrading/STOCK_DAY?date={month}&stockNo={stock.StockCode}&response=json";
                        HttpResponseMessage response = await _httpClient.GetAsync(url);
                        string responseBody = await response.Content.ReadAsStringAsync();
                        TwseStockExchangeReport data = JsonConvert.DeserializeObject<TwseStockExchangeReport>(responseBody);
                        foreach (var i in data.Data)
                        {
                            StockTechData techData = new StockTechData()
                            {
                                Date = DateTime.ParseExact((int.Parse(i[0].Replace("/", "")) + 19110000).ToString(), "yyyyMMdd", null),
                                Volume = int.Parse(i[1].Replace(",", "")),
                                Open = decimal.Parse(i[3]),
                                High = decimal.Parse(i[4]),
                                Low = decimal.Parse(i[5]),
                                Close = decimal.Parse(i[6]),
                            };
                            stock.TechDataList.Add(techData);
                        }
                    }
                    Console.WriteLine($"Retrieve exchange report of Stock {stock.StockCode} {stock.CompanyName} finished.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurs while retrieving exchange report of Stock {stock.StockCode} {stock.CompanyName}. Error message: {ex}");
                }
            }
        }
        private async Task DeleteActiveCandidate(List<Candidate> candidateList)
        {
            Dictionary<string, Candidate> candidateDict = candidateList.ToDictionary(x => x.StockCode);
            List<Candidate> activeCandidateList = await _candidateRepository.GetActiveCandidate();
            if (!activeCandidateList.Any()) return;
            List<Guid> candidateToDeleteList = new List<Guid>();
            List<Guid> duplicateActiveCandidate = activeCandidateList.GroupBy(x => x.StockCode).SelectMany(g => g.OrderByDescending(x => x.SelectDate).Skip(1)).Select(x=>x.Id).ToList();
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
                        List<StockTechData> orderedTechDataList = stock.OrderedTechDataList;
                        if (orderedTechDataList.First().Close < orderedTechDataList.Take(10).Average(x=>x.Close) &
                            orderedTechDataList.First().Close < i.GapUpLow)
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
    }
}