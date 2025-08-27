using Core.HttpClientFactory;
using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Service
{
    public class StockSelectorService : IStockSelectorService
    {
        private HttpClient _httpClient;
        private readonly ICandidateRepository _candidateRepository;
        private readonly ILogger _logger;
        private readonly IDateTimeService _dateTimeService;
        private readonly IDiscordService _discordService;
        private int _maxRetryCount = 10;
        public StockSelectorService(ICandidateRepository candidateRepository, ILogger logger, IDateTimeService dateTimeService, IDiscordService discordService)
        {
            SimpleHttpClientFactory httpClientFactory = new SimpleHttpClientFactory();
            _httpClient = httpClientFactory.CreateClient();
            _candidateRepository = candidateRepository;
            int maxConcurrency = Environment.ProcessorCount * 40;
            _logger = logger;
            _dateTimeService = dateTimeService;
            _discordService = discordService;
        }
        public async Task SelectStock()
        {
            List<StockCandidate> allStockInfoList = await GetStockCodeList();
            await SetExchangeReportFromSino(allStockInfoList);
            if (!doesNeedUpdate(allStockInfoList)) return;
            List<StockCandidate> candidateList = SelectCandidateByTech(allStockInfoList);
            candidateList = await SelectCandidateByMainPower(candidateList);
            Dictionary<string, StockCandidate> allStockInfoDict = allStockInfoList.ToDictionary(x => x.StockCode);
            await UpdateCandidate(candidateList, allStockInfoDict);
            await UpdateExRightsExDevidendDate();
            await UpSertTechDataToDb(allStockInfoList);
            //List<StockCandidate> dailyExchangeReport = await GetDailyExchangeReportFromTwseAndTwotc();
            //await UpdateTrade(allStockInfoDict);
            //await UpdateCrazyCandidate(crazyCandidateList, allStockInfoDict);
            //List<StockCandidate> crazyCandidateList = SelectCrazyCandidate(allStockInfoList);
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
            _logger.Information("Get TWSE stock code started.");
            List<StockCandidate> stockList = new List<StockCandidate>();
            int retryCount = 0;
            while (retryCount <= _maxRetryCount)
            {
                try
                {
                    HttpResponseMessage response = await _httpClient.GetAsync("https://openapi.twse.com.tw/v1/opendata/t187ap03_L");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    List<TwseStockInfo> stockInfoList = JsonConvert.DeserializeObject<List<TwseStockInfo>>(responseBody);
                    HttpResponseMessage intradayResponse = await _httpClient.GetAsync("https://openapi.twse.com.tw/v1/exchangeReport/TWTB4U");
                    string intradayResponseBody = await intradayResponse.Content.ReadAsStringAsync();
                    List<TwseIntradayStockInfo> intradayStockInfoList = JsonConvert.DeserializeObject<List<TwseIntradayStockInfo>>(intradayResponseBody);
                    HashSet<string> intradayStockCodeHashSet = new HashSet<string>(intradayStockInfoList.Select(x => x.Code.ToUpper()));
                    stockList = stockInfoList.Where(x => intradayStockCodeHashSet.Contains(x.公司代號.ToUpper())).Select(x => new StockCandidate()
                    {
                        Market = enumMarketType.TWSE,
                        StockCode = x.公司代號.ToUpper(),
                        CompanyName = x.公司簡稱
                    }).ToList();
                    break;
                }
                catch(Exception ex)
                {
                    _logger.Error($"Error occurs while retrieving TWSE stock code. Error message: {ex.Message}");
                    if (retryCount >= _maxRetryCount) throw;
                    retryCount++;
                }
            }
            _logger.Information("Get TWSE stock code finished.");
            return stockList;
        }
        private async Task<List<StockCandidate>> GetTwotcStockCode()
        {
            _logger.Information("Get TWOTC stock code started.");
            List<StockCandidate> stockList = new List<StockCandidate>();
            int retryCount = 0;
            while (retryCount <= _maxRetryCount)
            {
                try
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
                    stockList = stockInfoList.Where(x => intradayStockCodeHashSet.Contains(x.SecuritiesCompanyCode.ToUpper())).Select(x => new StockCandidate()
                    {
                        Market = enumMarketType.TWOTC,
                        StockCode = x.SecuritiesCompanyCode.ToUpper(),
                        CompanyName = x.CompanyAbbreviation
                    }).ToList();
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error occurs while retrieving TWOTC stock code. Error message: {ex.Message}");
                    if (retryCount >= _maxRetryCount) throw;
                    retryCount++;
                }
            }
            _logger.Information("Get TWOTC stock code finished.");
            return stockList;
        }
        private async Task<List<StockCandidate>> SelectCandidateByMainPower(List<StockCandidate> candidateList)
        {
            _logger.Information("Retrieve main power started.");
            List<StockCandidate> stockMatchMainPowerList = new List<StockCandidate>();
            foreach (var stock in candidateList)
            {
                try
                {
                    int retryCount = 0;
                    while (retryCount <= _maxRetryCount)
                    {
                        try
                        {
                            string url = $"https://stockchannelnew.sinotrade.com.tw/Z/ZC/ZCO/CZCO.DJBCD?A={stock.StockCode}";
                            HttpResponseMessage response = await _httpClient.GetAsync(url);
                            string responseBody = await response.Content.ReadAsStringAsync();
                            var parts = responseBody.Split(' ');
                            string[] mainPowerArray = parts[2].Split(',');
                            if (mainPowerArray.Length < 5) throw new Exception($"Main power data is not enough for stock {stock.StockCode}.");
                            int count = 0;
                            for (var j = 1; j <= 5; j++)
                            {
                                if (int.TryParse(mainPowerArray[mainPowerArray.Length - j], out int mainPower) && mainPower > 0) count++;
                            }
                            if (count >= 5)
                            {
                                stockMatchMainPowerList.Add(stock);
                            }
                            break;
                        }
                        catch
                        {
                            if (retryCount >= _maxRetryCount) throw;
                            retryCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error occurs while retrieving main power of Stock {stock.Market} {stock.StockCode} {stock.CompanyName}. Error message: {ex}");
                }
            }
            _logger.Information("Retrieve main power finished.");
            return stockMatchMainPowerList;
        }
        private async Task SetExchangeReportFromSino(List<StockCandidate> stockList)
        {
            _logger.Information("Retrieve exchange report started.");
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
                    _logger.Error($"Error occurs while retrieving exchange report of Stock {stock.Market} {stock.StockCode} {stock.CompanyName}. Error message: {ex.Message}");
                }
            }
            _logger.Information("Retrieve exchange report finished.");
        }
        private async Task UpSertTechDataToDb(List<StockCandidate> stockList)
        {
            List<StockTech> stockTechList = stockList.Select(x => new StockTech
            {
                StockCode = x.StockCode,
                CompanyName = x.CompanyName,
                TechData = JsonConvert.SerializeObject(x.TechDataList)
            }).ToList();
            await _candidateRepository.UpsertStockTech(stockTechList);
        }
        private List<StockCandidate> SelectCandidateByTech(List<StockCandidate> allStockInfoList)
        {
            List<StockCandidate> candidateList = new List<StockCandidate>();
            foreach (var i in allStockInfoList)
            {
                i.IsCandidate = IsCandidate(i.TechDataList, out decimal gapUpHigh, out decimal gapUpLow);
                if (!i.IsCandidate || gapUpHigh == 0 || gapUpLow == 0) continue;
                i.GapUpHigh = gapUpHigh;
                i.GapUpLow = gapUpLow;
                i.SelectedDate = i.TechDataList.First().Date;
                i.EntryPoint = GetEntryPoint(i.GapUpHigh);
                i.StopLossPoint = GetStopLossPoint(i.GapUpHigh);
                i.Last9TechData = JsonConvert.SerializeObject(i.TechDataList.Take(9));
                candidateList.Add(i);
            }
            return candidateList;
        }
        private bool IsCandidate(List<StockTechData> techDataList, out decimal gapUpHigh, out decimal gapUpLow)
        {
            gapUpHigh = 0;
            gapUpLow = 0;
            if (techDataList.Count < 100) return false;
            int stableCount = 5;
            List<StockTechData> latestStableTech = techDataList.Take(stableCount).ToList();
            decimal latestStableCloseMax = latestStableTech.Max(x => x.Close);
            decimal latestStableCloseMin = latestStableTech.Min(x => x.Close);
            decimal prev4High = techDataList.Skip(1).Take(4).Max(x => x.High);
            decimal ma20 = techDataList.Take(20).Average(x => x.Close);
            decimal ma60 = techDataList.Take(60).Average(x => x.Close);
            decimal mv5 = (decimal)latestStableTech.Average(x => x.Volume);
            decimal todayClose = techDataList.First().Close;
            if (latestStableCloseMax / latestStableCloseMin > 1.02m || mv5 < 1000 || todayClose / ma20 > 1.05m || todayClose < ma60 || todayClose > prev4High) return false;
            for (int i = 0; i < stableCount - 1; i++)
            {
                decimal baseHigh = latestStableTech[i].High;
                decimal baseLow = latestStableTech[i].Low;
                for (int j = i + 1; j < stableCount; j++)
                {
                    if (!(baseHigh >= latestStableTech[j].Low && latestStableTech[j].High >= baseLow))
                    {
                        return false;
                    }
                }
            }
            gapUpHigh = latestStableTech.Max(x => x.High);
            gapUpLow = latestStableTech.Min(x => x.Low);
            return true;
        }

        private decimal GetEntryPoint(decimal gapUpHigh)
        {
            decimal tick = 0;
            if (gapUpHigh < 10)
            {
                tick = 0.01m;
            }
            else if (gapUpHigh < 50)
            {
                tick = 0.05m;
            }
            else if (gapUpHigh < 100)
            {
                tick = 0.1m;
            }
            else if (gapUpHigh < 500)
            {
                tick = 0.5m;
            }
            else if (gapUpHigh < 1000)
            {
                tick = 1m;
            }
            else
            {
                tick = 5m;
            }
            return gapUpHigh + tick;
        }
        private decimal GetStopLossPoint(decimal gapUpHigh)
        {
            decimal tick = 0;
            if (gapUpHigh <= 10)
            {
                tick = 0.01m;
            }
            else if (gapUpHigh <= 50)
            {
                tick = 0.05m;
            }
            else if (gapUpHigh <= 100)
            {
                tick = 0.1m;
            }
            else if (gapUpHigh <= 500)
            {
                tick = 0.5m;
            }
            else if (gapUpHigh <= 1000)
            {
                tick = 1m;
            }
            else
            {
                tick = 5m;
            }
            return gapUpHigh - tick;
        }

        private async Task UpdateCandidate(List<StockCandidate> candidateToInsertList, Dictionary<string, StockCandidate> allStockInfoDict)
        {
            List<StockCandidate> activeCandidateList = await _candidateRepository.GetActiveCandidate();
            Dictionary<string, StockCandidate> candidateToInsertDict = candidateToInsertList.ToDictionary(x => x.StockCode);
            List<StockCandidate> candidateToDeleteList = new List<StockCandidate>();
            List<StockCandidate> candidateToUpdateList = new List<StockCandidate>();
            List<StockCandidate> errorCandidateList = new List<StockCandidate>();
            foreach (var i in activeCandidateList)
            {
                bool isDuplicateCandidate = candidateToInsertDict.ContainsKey(i.StockCode);
                bool hasLatestStockInfo = allStockInfoDict.TryGetValue(i.StockCode, out StockCandidate stock);
                if (i.PurchasedLot > 0)
                {
                    if (isDuplicateCandidate)
                    {
                        candidateToInsertList.Remove(candidateToInsertDict[i.StockCode]);
                    }
                    if (hasLatestStockInfo && stock.TechDataList.Count >= 9)
                    {
                        i.Last9TechData = JsonConvert.SerializeObject(stock.TechDataList.Take(9));
                        candidateToUpdateList.Add(i);
                    }
                }
                else
                {
                    if (isDuplicateCandidate)
                    {
                        candidateToDeleteList.Add(i);
                        continue;
                    }
                    if (!hasLatestStockInfo || stock.TechDataList.Count < 9) 
                    {
                        errorCandidateList.Add(i);
                        _logger.Error($"Can not retrieve last 9 tech data of stock code {i.StockCode}.");
                        continue;
                    } 
                    decimal todayClose = stock.TechDataList.First().Close;
                    if (todayClose < i.GapUpLow || (todayClose > i.EntryPoint && todayClose < stock.TechDataList.Take(20).Average(x => x.Close)))
                    {
                        candidateToDeleteList.Add(i);
                        continue;
                    }
                    i.Last9TechData = JsonConvert.SerializeObject(stock.TechDataList.Take(9));
                    candidateToUpdateList.Add(i);
                }
            }
            List<Guid> candidateIdToDeleteList = candidateToDeleteList.Select(x => x.Id).ToList();
            await _candidateRepository.UpdateCandidate(candidateIdToDeleteList, candidateToUpdateList, candidateToInsertList);
            await SendCandidateToDiscord(candidateToDeleteList, candidateToUpdateList, candidateToInsertList, errorCandidateList);
        }
        private async Task SendCandidateToDiscord(List<StockCandidate> candidateToDeleteList, List<StockCandidate> candidateToUpdateList, List<StockCandidate> candidateToInsertList, List<StockCandidate> errorCandidateList)
        {
            string message = "";
            message += $"Removed candidates: {candidateToDeleteList.Count}\n";
            foreach (var i in candidateToDeleteList)
            {
                message += $"{i.StockCode} {i.CompanyName} {i.EntryPoint}\n";
            }
            message += $"New candidates: {candidateToInsertList.Count}\n";
            foreach (var i in candidateToInsertList)
            {
                message += $"{i.StockCode} {i.CompanyName} {i.EntryPoint}\n";
            }
            message += $"Error candidates: {errorCandidateList.Count}\n";
            foreach (var i in errorCandidateList)
            {
                message += $"{i.StockCode} {i.CompanyName} {i.EntryPoint}\n";
            }
            message += $"All candidates: {candidateToUpdateList.Count + candidateToInsertList.Count}\n";
            foreach (var i in candidateToUpdateList)
            {
                message += $"{i.StockCode} {i.CompanyName} {i.EntryPoint}\n";
            }
            foreach (var i in candidateToInsertList)
            {
                message += $"{i.StockCode} {i.CompanyName} {i.EntryPoint}\n";
            }
            await _discordService.SendMessage(message);
        }

        private async Task UpdateExRightsExDevidendDate()
        {
            List<ExRrightsExDividend> twseExRrightsExDividendList = await GetTwseExRightsExDevidendDate();
            List<ExRrightsExDividend> twotcExRrightsExDividendList = await GetTwotcExRightsExDevidendDate();
            List<ExRrightsExDividend> allExRrightsExDividendList = twseExRrightsExDividendList.Concat(twotcExRrightsExDividendList).ToList();
            Dictionary<string, ExRrightsExDividend> allExRrightsExDividendDict = new Dictionary<string, ExRrightsExDividend>();
            foreach (var i in allExRrightsExDividendList)
            {
                if (i.ExRrightsExDividendDateTime.Date <= _dateTimeService.GetTaiwanTime().Date) continue;
                if (allExRrightsExDividendDict.TryGetValue(i.StockCode, out ExRrightsExDividend exRrightsExDividend))
                {
                    if (i.ExRrightsExDividendDateTime < exRrightsExDividend.ExRrightsExDividendDateTime)
                    {
                        allExRrightsExDividendDict[i.StockCode] = i;
                    }
                }
                else
                {
                    allExRrightsExDividendDict.Add(i.StockCode, i);
                }
            }
            List<StockCandidate> candidateList = await _candidateRepository.GetActiveCandidate();
            foreach (var i in candidateList)
            {
                if (allExRrightsExDividendDict.TryGetValue(i.StockCode, out ExRrightsExDividend exRrightsExDividend))
                {
                    i.ExRrightsExDividendDateTime = exRrightsExDividend.ExRrightsExDividendDateTime;
                }
            }
            await _candidateRepository.UpdateExRrightsExDividendDate(candidateList);
        }
        private async Task<List<ExRrightsExDividend>> GetTwseExRightsExDevidendDate()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("https://openapi.twse.com.tw/v1/exchangeReport/TWT48U_ALL");
            string responseBody = await response.Content.ReadAsStringAsync();
            List<TwseExRrightsExDividend> exRrightsExDividendList = JsonConvert.DeserializeObject<List<TwseExRrightsExDividend>>(responseBody);
            List<ExRrightsExDividend> result = exRrightsExDividendList.Select(x => new ExRrightsExDividend
            {
                StockCode = x.Code.ToUpper(),
                ExRrightsExDividendDateTime = _dateTimeService.ConvertTaiwaneseCalendarToGregorianCalendar(x.Date)
            }).ToList();
            return result;
        }
        private async Task<List<ExRrightsExDividend>> GetTwotcExRightsExDevidendDate()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("https://www.tpex.org.tw/openapi/v1/tpex_exright_prepost");
            string responseBody = await response.Content.ReadAsStringAsync();
            List<TwotcExRrightsExDividend> exRrightsExDividendList = JsonConvert.DeserializeObject<List<TwotcExRrightsExDividend>>(responseBody);
            List<ExRrightsExDividend> result = exRrightsExDividendList.Select(x => new ExRrightsExDividend
            {
                StockCode = x.SecuritiesCompanyCode.ToUpper(),
                ExRrightsExDividendDateTime = _dateTimeService.ConvertTaiwaneseCalendarToGregorianCalendar(x.ExRrightsExDividendDate)
            }).ToList();
            return result;
        }
        private bool doesNeedUpdate(List<StockCandidate> stockList)
        {
            DateTime latestTechDate = stockList.SelectMany(x => x.TechDataList).Max(x => x.Date);
            if (_dateTimeService.GetTaiwanTime().Date > latestTechDate.Date) return false;
            return true;
        }
        private async Task<List<StockCandidate>> GetDailyExchangeReportFromTwseAndTwotc()
        {
            List<StockCandidate> dailyTechDataList = new List<StockCandidate>();
            var twseDailyExchangeReport = await GetDailyExchangeReportFromTwse();
            var twotcDailyExchangeReport = await GetDailyExchangeReportFromTwotc();
            dailyTechDataList.AddRange(twseDailyExchangeReport);
            dailyTechDataList.AddRange(twotcDailyExchangeReport);
            return dailyTechDataList;
        }
        private async Task<List<StockCandidate>> GetDailyExchangeReportFromTwse()
        {
            _logger.Information("Get TWSE stock daily exchange report started.");
            HttpResponseMessage response = await _httpClient.GetAsync("https://openapi.twse.com.tw/v1/exchangeReport/STOCK_DAY_ALL");
            string responseBody = await response.Content.ReadAsStringAsync();
            List<TwseTechData> stockTech = JsonConvert.DeserializeObject<List<TwseTechData>>(responseBody);
            List<StockCandidate> twseDailyExchangeReport = stockTech.Select(x => new StockCandidate()
            {
                StockCode = x.Code.ToUpper(),
                TechDataList = new List<StockTechData>()
                {
                    new StockTechData() {
                    Close = decimal.TryParse(x.ClosingPrice, out decimal close) ? close : 0,
                    Open = decimal.TryParse(x.OpeningPrice, out decimal open) ? open : 0,
                    High = decimal.TryParse(x.HighestPrice, out decimal high) ? high : 0,
                    Low = decimal.TryParse(x.LowestPrice, out decimal low) ? low : 0,
                    Volume = int.TryParse(x.TradeVolume, out int volume) ? volume : 0,
                    Date = _dateTimeService.ConvertTaiwaneseCalendarToGregorianCalendar(x.Date)
                    }
                }
            }).ToList();
            _logger.Information("Get TWSE stock daily exchange report finished.");
            return twseDailyExchangeReport;
        }
        private async Task<List<StockCandidate>> GetDailyExchangeReportFromTwotc()
        {
            _logger.Information("Get TWOTC stock daily exchange report started.");
            HttpResponseMessage response = await _httpClient.GetAsync("https://www.tpex.org.tw/openapi/v1/tpex_mainboard_quotes");
            string responseBody = await response.Content.ReadAsStringAsync();
            List<TwotcTechData> stockTech = JsonConvert.DeserializeObject<List<TwotcTechData>>(responseBody);
            List<StockCandidate> twotcDailyExchangeReport = stockTech.Select(x => new StockCandidate()
            {
                StockCode = x.SecuritiesCompanyCode.ToUpper(),
                TechDataList = new List<StockTechData>()
                {
                    new StockTechData()
                    {
                        Close = decimal.TryParse(x.Close, out decimal close) ? close : 0,
                        Open = decimal.TryParse(x.Open, out decimal open) ? open : 0,
                        High = decimal.TryParse(x.High, out decimal high) ? high : 0,
                        Low = decimal.TryParse(x.Low, out decimal low) ? low : 0,
                        Volume = int.TryParse(x.TradingShares, out int volume) ? volume / 1000 : 0,
                        Date = _dateTimeService.ConvertTaiwaneseCalendarToGregorianCalendar(x.Date)
                    }
                }
            }).ToList();
            _logger.Information("Get TWOTC stock daily exchange report finished.");
            return twotcDailyExchangeReport;
        }
        private List<StockCandidate> SelectCrazyCandidate(List<StockCandidate> stockList)
        {
            List<StockCandidate> candidateList = new List<StockCandidate>();
            foreach (var i in stockList)
            {
                i.IsCandidate = IsCrazyCandidate(i.TechDataList, out StockTechData stockTechData);
                if (!i.IsCandidate || stockTechData == null) continue;
                i.SelectedDate = i.TechDataList.First().Date;
                i.EntryPoint = GetEntryPoint(stockTechData.High);
                i.StopLossPoint = GetStopLossPoint(stockTechData.High);
                i.Last9TechData = JsonConvert.SerializeObject(i.TechDataList.Take(9));
                candidateList.Add(i);
            }
            return candidateList;
        }
        private bool IsCrazyCandidate(List<StockTechData> techDataList, out StockTechData stockTechData)
        {
            stockTechData = null;
            if (techDataList.Count < 20) return false;
            stockTechData = techDataList.First();
            decimal ma5 = techDataList.Take(5).Average(x => x.Close);
            double privMv5 = techDataList.Skip(1).Take(5).Average(x => x.Volume);
            bool fitVolumeAlready = false;
            for (int i = 1; i < 6; i++)
            {
                if (techDataList[i].Volume >= techDataList.Skip(i + 1).Take(5).Average(x => x.Volume) * 5)
                {
                    fitVolumeAlready = true;
                    break;
                }
            }
            if (!fitVolumeAlready &&
                stockTechData.Close == stockTechData.High &&
                stockTechData.Close / techDataList[1].Close > 1.095m &&
                stockTechData.Volume >= privMv5 * 5 &&
                stockTechData.Close >= ma5 &&
                stockTechData.Close / ma5 <= 1.1m)
            {
                return true;
            }
            return false;
        }
        private async Task UpdateCrazyCandidate(List<StockCandidate> candidateToInsertList, Dictionary<string, StockCandidate> allStockInfoDict)
        {
            List<StockCandidate> activeCrazyCandidateList = await _candidateRepository.GetActiveCrazyCandidate();
            Dictionary<string, StockCandidate> candidateDict = candidateToInsertList.ToDictionary(x => x.StockCode);
            List<Guid> candidateToDeleteList = new List<Guid>();
            List<StockCandidate> candidateToUpdateList = new List<StockCandidate>();
            foreach (var i in activeCrazyCandidateList)
            {
                if (candidateDict.ContainsKey(i.StockCode))
                {
                    candidateToDeleteList.Add(i.Id);
                    continue;
                }
                if (!allStockInfoDict.TryGetValue(i.StockCode, out StockCandidate stock))
                {
                    candidateToDeleteList.Add(i.Id);
                    continue;
                }
                if (stock.TechDataList.Count < 9)
                {
                    candidateToDeleteList.Add(i.Id);
                    continue;
                }
                decimal todayClose = stock.TechDataList.First().Close;
                if (todayClose < stock.TechDataList.Take(5).Average(x => x.Close))
                {
                    candidateToDeleteList.Add(i.Id);
                    continue;
                }
                i.Last9TechData = JsonConvert.SerializeObject(stock.TechDataList.Take(9));
                candidateToUpdateList.Add(i);
            }
            await _candidateRepository.UpdateCrazyCandidate(candidateToDeleteList, candidateToUpdateList, candidateToInsertList);
        }
        private string ConvertMarketFromYuantaToYahoo(enumMarketType enumMarketType)
        {
            return enumMarketType == enumMarketType.TWSE ? "TW" : "TWO";
        }
        //private async Task UpdateTrade(Dictionary<string, StockCandidate> allStockInfoDict)
        //{
        //    List<StockTrade> stockHoldingList = await _tradeRepository.GetStockHolding();
        //    foreach (var i in stockHoldingList)
        //    {
        //        if (!allStockInfoDict.TryGetValue(i.StockCode, out StockCandidate stock) || stock.TechDataList.Count < 9)
        //        {
        //            _logger.Error($"Can not retrieve last 9 tech data of stock code {i.StockCode}.");
        //            continue;
        //        }
        //        i.Last9TechData = JsonConvert.SerializeObject(stock.TechDataList.Take(9));
        //    }
        //    await _tradeRepository.UpdateLast9TechData(stockHoldingList);
        //}
    }
}