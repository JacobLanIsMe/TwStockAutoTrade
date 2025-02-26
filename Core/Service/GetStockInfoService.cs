using Core.Enum;
using Core.HttpClientFactory;
using Core.Interface;
using Core.Model;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Service
{
    public class GetStockInfoService : IGetStockInfoService
    {
        private HttpClient _httpClient;
        private readonly IDateTimeService _dateTimeService;
        public GetStockInfoService(IDateTimeService dateTimeService)
        {
            SimpleHttpClientFactory httpClientFactory = new SimpleHttpClientFactory();
            _httpClient = httpClientFactory.CreateClient();
            _dateTimeService = dateTimeService;
        }
        public async Task SelectStock()
        {
            var twseStockListTask = GetTwseStockCode();
            var tpexStockListTask = GetTpexStockCode();
            var twseStockList = await twseStockListTask;
            var tpexStockList = await tpexStockListTask;
            //await GetTwseDailyExchangeRecort(twseStockList);
            List<Stock> mergedStockList = twseStockList.Concat(tpexStockList).ToList();
            List<Stock> candidateList = await GetSelectedStock(mergedStockList);

        }
        private async Task<List<Stock>> GetTwseStockCode()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("https://openapi.twse.com.tw/v1/opendata/t187ap03_L");
            string responseBody = await response.Content.ReadAsStringAsync();
            List<TwseStockInfo> stockInfoList = JsonConvert.DeserializeObject<List<TwseStockInfo>>(responseBody);
            List<Stock> stockList = new List<Stock>();
            foreach (var i in stockInfoList)
            {
                if (!int.TryParse(i.公司代號, out int stockCode)) continue;
                Stock stock = new Stock()
                {
                    Market = EMarket.TWSE,
                    StockCode = stockCode,
                    CompanyName = i.公司簡稱
                };
                stockList.Add(stock);
            }
            return stockList;
        }
        private async Task<List<Stock>> GetTpexStockCode()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("https://www.tpex.org.tw/openapi/v1/mopsfin_t187ap03_O");
            string responseBody = await response.Content.ReadAsStringAsync();
            List<TpexStockInfo> stockInfoList = JsonConvert.DeserializeObject<List<TpexStockInfo>>(responseBody);
            List<Stock> stockList = new List<Stock>();
            foreach (var i in stockInfoList)
            {
                if (!int.TryParse(i.SecuritiesCompanyCode, out int stockCode)) continue;
                Stock stock = new Stock()
                {
                    Market = EMarket.TPEX,
                    StockCode = stockCode,
                    CompanyName = i.CompanyAbbreviation,
                };
                stockList.Add(stock);
            }
            return stockList;
        }
        private async Task GetTwseDailyExchangeRecort(List<Stock> twseStockList)
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
        private async Task<List<Stock>> GetSelectedStock(List<Stock> stockList)
        {
            ConcurrentBag<Stock> candidateList = new ConcurrentBag<Stock>();
            int maxConcurrency = Environment.ProcessorCount * 40;
            SemaphoreSlim _semaphore = new SemaphoreSlim(maxConcurrency);
            var tasks = stockList.Select(async i =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (i.TechDataList.Count < 25) return;
                    List<StockTechData> orderedTechDataList = i.TechDataList.OrderByDescending(x => x.Date).ToList();
                    StockTechData gapUpStockTechData = orderedTechDataList[4];
                    if (gapUpStockTechData.Low <= orderedTechDataList[5].High) return; // Gap up
                    double mv5 = orderedTechDataList.Take(5).Average(x => x.Volume);
                    if (mv5 < 100) return;
                    decimal volatility = orderedTechDataList.Take(5).Max(x => x.Close) / orderedTechDataList.Take(5).Min(x => x.Close);
                    if (volatility > (decimal)1.02) return;
                    decimal gapUpMa5 = orderedTechDataList.Skip(4).Take(5).Average(x => x.Close);
                    decimal gapUpMa10 = orderedTechDataList.Skip(4).Take(10).Average(x => x.Close);
                    decimal gapUpMa20 = orderedTechDataList.Skip(4).Take(20).Average(x => x.Close);
                    if (gapUpStockTechData.Close < gapUpMa5 || gapUpStockTechData.Close < gapUpMa10 || gapUpStockTechData.Close < gapUpMa20) return;
                    List<decimal> last4Close = orderedTechDataList.Take(4).Select(x => x.Close).ToList();
                    bool isPeriodCloseHigherThanGapUpHigh = last4Close.Max() > gapUpStockTechData.High;
                    bool isPeriodCloseLowerThanGapUpLow = last4Close.Min() < gapUpStockTechData.Low;
                    if (isPeriodCloseHigherThanGapUpHigh || isPeriodCloseLowerThanGapUpLow) return;
                    candidateList.Add(i);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurs while filtering stock {i.StockCode} {i.CompanyName}. Error message: {ex}");
                }
                finally
                {
                    _semaphore.Release();
                }
            });
            await Task.WhenAll(tasks);
            return candidateList.ToList();
        }
    }
}