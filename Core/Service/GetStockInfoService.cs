using Core.Enum;
using Core.HttpClientFactory;
using Core.Interface;
using Core.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Core.Service
{
    public class GetStockInfoService : IGetStockInfoService
    {
        private HttpClient _httpClient;
        public GetStockInfoService()
        {
            SimpleHttpClientFactory httpClientFactory = new SimpleHttpClientFactory();
            _httpClient = httpClientFactory.CreateClient();
        }
        public async Task SelectStock()
        {
            var twseStockListTask = GetTwseStockCode();
            var tpexStockListTask = GetTpexStockCode();
            var twseStockList = await twseStockListTask;
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
                    StockCode = i.SecuritiesCompanyCode,
                    CompanyName = i.CompanyAbbreviation,
                };
                stockList.Add(stock);
            }
            return stockList;
        }
        private async Task Get
    }
}