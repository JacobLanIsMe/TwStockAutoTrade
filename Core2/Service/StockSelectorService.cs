using Core2.Enum;
using Core2.HttpClientFactory;
using Core2.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;

namespace Core2.Service
{
    public class StockSelectorService
    {
        private HttpClient _httpClient;
        private readonly DiscordService _discordService;
        private readonly MongoDbService _mongoService;
        private int _maxRetryCount = 10;
        private readonly ILogger<StockSelectorService> _logger;
        public StockSelectorService(DiscordService discordService, ILogger<StockSelectorService> logger, MongoDbService mongoService)
        {
            SimpleHttpClientFactory httpClientFactory = new SimpleHttpClientFactory();
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _discordService = discordService;
            _mongoService = mongoService;
        }
        public async Task SelectStock()
        {
            List<StockCandidate> allStockInfoList = await GetStockCodeList();
            await SetIssuedShares(allStockInfoList);
            await SetExchangeReportFromSino(allStockInfoList);
            await SelectBreakoutStock(allStockInfoList);
            await UpSertTechDataToDb(allStockInfoList);
        }
        private async Task<List<StockCandidate>> GetStockCodeList()
        {
            var twseStockListTask = GetTwseStockCode();
            var tpexStockListTask = GetTwotcStockCode();
            var twseStockList = await twseStockListTask;
            var tpexStockList = await tpexStockListTask;
            List<StockCandidate> allStockInfoList = twseStockList.Concat(tpexStockList).ToList();
            return allStockInfoList;
        }
        private async Task<List<StockCandidate>> GetTwseStockCode()
        {
            _logger.LogInformation("Get TWSE stock code started.");
            List<StockCandidate> stockList = new List<StockCandidate>();
            int retryCount = 0;
            while (retryCount <= _maxRetryCount)
            {
                try
                {
                    HttpResponseMessage response = await _httpClient.GetAsync("https://openapi.twse.com.tw/v1/opendata/t187ap03_L");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    List<TwseStockInfo> stockInfoList = JsonSerializer.Deserialize<List<TwseStockInfo>>(responseBody);
                    stockList = stockInfoList.Select(x => new StockCandidate()
                    {
                        Market = EMarketType.TWSE,
                        StockCode = x.公司代號.ToUpper(),
                        CompanyName = x.公司簡稱
                    }).ToList();
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error occurs while retrieving TWSE stock code. Error message: {ex.Message}");
                    if (retryCount >= _maxRetryCount) throw;
                    retryCount++;
                }
            }
            _logger.LogInformation("Get TWSE stock code finished.");
            return stockList;
        }
        private async Task<List<StockCandidate>> GetTwotcStockCode()
        {
            _logger.LogInformation("Get TWOTC stock code started.");
            List<StockCandidate> stockList = new List<StockCandidate>();
            int retryCount = 0;
            while (retryCount <= _maxRetryCount)
            {
                try
                {
                    HttpResponseMessage response = await _httpClient.GetAsync("https://www.tpex.org.tw/openapi/v1/mopsfin_t187ap03_O");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    List<TwotcStockInfo> stockInfoList = JsonSerializer.Deserialize<List<TwotcStockInfo>>(responseBody);
                    stockList = stockInfoList.Select(x => new StockCandidate()
                    {
                        Market = EMarketType.TWOTC,
                        StockCode = x.SecuritiesCompanyCode.ToUpper(),
                        CompanyName = x.CompanyAbbreviation
                    }).ToList();
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error occurs while retrieving TWOTC stock code. Error message: {ex.Message}");
                    if (retryCount >= _maxRetryCount) throw;
                    retryCount++;
                }
            }
            _logger.LogInformation("Get TWOTC stock code finished.");
            return stockList;
        }
        private async Task SetIssuedShares(List<StockCandidate> allStockInfoList)
        {
            #region 抓上市櫃公司基本資料
            HttpResponseMessage twseResponse = await _httpClient.GetAsync("https://openapi.twse.com.tw/v1/opendata/t187ap03_L");
            string twseResponseBody = await twseResponse.Content.ReadAsStringAsync();
            List<TwseCompanyInfo> twseCompanyInfoList = JsonSerializer.Deserialize<List<TwseCompanyInfo>>(twseResponseBody);
            HttpResponseMessage twotcResponse = await _httpClient.GetAsync("https://www.tpex.org.tw/openapi/v1/mopsfin_t187ap03_O");
            string twotcResponseBody = await twotcResponse.Content.ReadAsStringAsync();
            List<TwotcCompanyInfo> twotcCompanyInfoList = JsonSerializer.Deserialize<List<TwotcCompanyInfo>>(twotcResponseBody);
            #endregion
            Dictionary<string, long> companyShareDict = new Dictionary<string, long>();
            foreach (var i in twseCompanyInfoList)
            {
                string key = i.StockCode.ToUpper();
                if (!companyShareDict.ContainsKey(key))
                {
                    companyShareDict.Add(key, i.IssuedShare);
                }
            }
            foreach (var i in twotcCompanyInfoList)
            {
                string key = i.SecuritiesCompanyCode.ToUpper();
                if (!companyShareDict.ContainsKey(key))
                {
                    companyShareDict.Add(key, i.IssuedShare);
                }
            }
            foreach (var i in allStockInfoList)
            {
                if (companyShareDict.TryGetValue(i.StockCode, out long issuedShare))
                {
                    i.IssuedShare = issuedShare;
                }
            }
        }
        private async Task SetExchangeReportFromSino(List<StockCandidate> stockList)
        {
            _logger.LogInformation("Retrieve exchange report started.");
            foreach (var stock in stockList)
            {
                try
                {
                    int retryCount = 0;
                    while (retryCount <= _maxRetryCount)
                    {
                        try
                        {
                            string url = $"https://stockchannelnew.sinotrade.com.tw/z/BCD/czkc1.djbcd?a={stock.StockCode}&b=D&c=2880&E=1&ver=5";
                            HttpResponseMessage response = await _httpClient.GetAsync(url);
                            string responseBody = await response.Content.ReadAsStringAsync();
                            var parts = responseBody.Split(' ');
                            string[] date = parts[0].Split(',');
                            string[] open = parts[1].Split(',');
                            string[] high = parts[2].Split(',');
                            string[] low = parts[3].Split(',');
                            string[] close = parts[4].Split(',');
                            string[] volume = parts[5].Split(',');
                            for (int i = 0; i < date.Length; i++)
                            {
                                stock.TechDataList.Add(new StockTechData
                                {
                                    Date = DateTime.ParseExact(date[i], "yyyy/MM/dd", CultureInfo.InvariantCulture),
                                    Close = decimal.Parse(close[i]),
                                    Open = decimal.Parse(open[i]),
                                    High = decimal.Parse(high[i]),
                                    Low = decimal.Parse(low[i]),
                                    Volume = int.Parse(volume[i])
                                });
                            }
                            stock.TechDataList = stock.TechDataList.OrderByDescending(x => x.Date).ToList();
                            break;
                        }
                        catch
                        {
                            if (retryCount >= _maxRetryCount) throw;
                            await Task.Delay(2000);
                            retryCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error occurs while retrieving exchange report of Stock {stock.Market} {stock.StockCode} {stock.CompanyName}. Error message: {ex.Message}");
                }
            }
            _logger.LogInformation("Retrieve exchange report finished.");
        }
        private async Task SelectBreakoutStock(List<StockCandidate> allStockInfoList)
        {
            List<StockCandidate> breakoutCandidateList = new List<StockCandidate>();
            //List<dynamic> twseMarginList = await FetchTwseMarginDataAsync(_httpClient);
            //List<dynamic> tpexMarginList = await FetchTpexMarginDataAsync(_httpClient);
            foreach (var i in allStockInfoList)
            {
                //decimal marginIncreaseRate = CalculateMarginIncreaseWithTpexFallback(twseMarginList, tpexMarginList, i.StockCode);
                if (i.TechDataList == null || i.TechDataList.Count < 60) continue;
                StockTechData today = i.TechDataList.First();
                decimal mv5 = (decimal)i.TechDataList.Take(5).Average(x => x.Volume);
                decimal ma60 = i.TechDataList.Take(60).Average(x => x.Close);
                bool isFirstDayBreakout = true;
                decimal preMa5CrossPrice = 0;
                for (int j = 1; j <= 5; j++)
                {
                    if (i.TechDataList[j].Close > i.TechDataList.Skip(j + 1).Take(40).Max(x => x.High))
                    {
                        isFirstDayBreakout = false;
                        break;
                    }
                }
                for (int j = 0; j < i.TechDataList.Count; j++)
                {
                    if (i.TechDataList[j].Close < i.TechDataList.Skip(j).Take(5).Average(x => x.Close))
                    {
                        preMa5CrossPrice = i.TechDataList[j].Close;
                        break;
                    }
                }
                if (today.Close > i.TechDataList.Skip(1).Take(40).Max(x => x.High) &&
                    isFirstDayBreakout &&
                    today.Close > ma60 &&
                    today.Close < ma60 * 1.3m &&
                    today.Volume > mv5 * 2 &&
                    today.Volume > 1500 &&
                    preMa5CrossPrice * 1.2m >= today.Close)
                {
                    breakoutCandidateList.Add(i);
                }
            }
            await SendBreakoutStockToDiscord(breakoutCandidateList);
            _logger.LogInformation($"Sync candidates to Db started.");
            try
            {
                await _mongoService.SyncCandidates(breakoutCandidateList);
                _logger.LogInformation($"Sync candidates to Db finished.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurs while syncing candidates to Db. Error message: {ex}");
            }
        }
        private async Task SendBreakoutStockToDiscord(List<StockCandidate> candidateList)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine($"突破股票:");
            foreach (var i in candidateList)
            {
                message.AppendLine($"{i.StockCode} {i.CompanyName}");
            }
            message.AppendLine($"總共 {candidateList.Count} 檔");
            await _discordService.SendMessage(message.ToString());
        }
        private async Task UpSertTechDataToDb(List<StockCandidate> stockList)
        {
            List<StockTech> stockTechList = stockList.Select(x => new StockTech
            {
                StockCode = x.StockCode,
                CompanyName = x.CompanyName,
                IssuedShare = x.IssuedShare,
                TechData = JsonSerializer.Serialize(x.TechDataList)
            }).ToList();
            // Upsert into MongoDB for better performance with large batches (use injected service)
            try
            {
                await _mongoService.UpsertStockTech(stockTechList);
                _logger.LogInformation("Upsert tech data to MongoDB finished.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurs while upserting tech data to MongoDB. Error message: {ex}");
            }
    }
}
