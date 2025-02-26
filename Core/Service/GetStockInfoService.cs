using Core.Enum;
using Core.HttpClientFactory;
using Core.Interface;
using Core.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
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
            await GetTwseDailyExchangeRecort(twseStockList);
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
                                Date = DateTime.ParseExact((int.Parse(i[0].Replace("/", "")) + 19110000).ToString(), "yyyMMdd", null),
                                Volume = int.Parse(i[1].Replace(",", "")),
                                Open = decimal.Parse(i[3]),
                                High = decimal.Parse(i[4]),
                                Low = decimal.Parse(i[5]),
                                Close = decimal.Parse(i[6]),
                            };
                            stock.TechDataList.Add(techData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurs while retrieving exchange report of Stock {stock.StockCode} {stock.CompanyName}. Error message: {ex}");
                }
                
            }
        }
    }
}