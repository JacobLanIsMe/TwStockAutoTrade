using Core.Model;
using Core.Repository;
using Core.Repository.Interface;
using Core.Service.Interface;
using Core.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using System.Net.Http;
using Core.HttpClientFactory;
using HtmlAgilityPack;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Security.Policy;
using System.Diagnostics.Eventing.Reader;

namespace TryNewSelector
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location))
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build())
                .AddSingleton<ILogger>(Log.Logger)
                .AddSingleton<IDateTimeService, DateTimeService>()
                .AddSingleton<ICandidateRepository, CandidateRepository>()
                .BuildServiceProvider();

            var candidateRepository = serviceProvider.GetRequiredService<ICandidateRepository>();
            List<StockTech> stockTech = await candidateRepository.GetStockTech();
            SimpleHttpClientFactory simpleHttpClientFactory = new SimpleHttpClientFactory();
            var httpClient = simpleHttpClientFactory.CreateClient();
            List<StockTech> candidateList = new List<StockTech>();
            foreach (var i in stockTech)
            {
                var techDataList = JsonConvert.DeserializeObject<List<StockTechData>>(i.TechData);
                var today = techDataList.First();
                var yesterday = techDataList.Skip(1).First();
                ApiResponse mainPower = await FetchMainInOutDetailsAsync(i.StockCode, 1, httpClient);
                if (mainPower != null && 
                    mainPower.Data != null && 
                    mainPower.Data.MainInDetails != null && 
                    mainPower.Data.MainInDetails.Any() && 
                    mainPower.Data.MainInDetails.First().SecuritiesCompSymbol == "9268" &&
                    today.Close / yesterday.Close > 1.095m &&
                    today.Close == today.High)
                {
                    Console.WriteLine($"StockCode: {i.StockCode}, Name: {i.CompanyName}");
                    candidateList.Add(i);
                }
            }
            foreach (var i in candidateList)
            {
                Console.WriteLine($"Candidate: {i.StockCode}");
            }
            Console.WriteLine($"Total Candidate: {candidateList.Count}");
            #region 抓上市櫃公司基本資料
            HttpResponseMessage twseResponse = await httpClient.GetAsync("https://openapi.twse.com.tw/v1/opendata/t187ap03_L");
            string twseResponseBody = await twseResponse.Content.ReadAsStringAsync();
            List<TwseCompanyInfo> twseCompanyInfoList = JsonConvert.DeserializeObject<List<TwseCompanyInfo>>(twseResponseBody);
            HttpResponseMessage twotcResponse = await httpClient.GetAsync("https://www.tpex.org.tw/openapi/v1/mopsfin_t187ap03_O");
            string twotcResponseBody = await twotcResponse.Content.ReadAsStringAsync();
            List<TwotcCompanyInfo> twotcCompanyInfoList = JsonConvert.DeserializeObject<List<TwotcCompanyInfo>>(twotcResponseBody);
            #endregion

            Dictionary<string, long> companyShareDict = new Dictionary<string, long>();
            foreach (var i in twseCompanyInfoList)
            {
                if (!companyShareDict.ContainsKey(i.StockCode))
                {
                    companyShareDict.Add(i.StockCode, i.IssuedShares);
                }
            }
            foreach (var i in twotcCompanyInfoList)
            {
                if (!companyShareDict.ContainsKey(i.SecuritiesCompanyCode))
                {
                    companyShareDict.Add(i.SecuritiesCompanyCode, i.IssuedShares);
                }
            }
            int winCount = 0;
            int loseCount = 0;
            int reachLimitUpCount = 0;
            decimal totalReturnRate = 0;
            object _lockObject = new object();
            string filePath = "C:\\Users\\Administrator\\Downloads\\result.csv";
            ConcurrentBag<string> results = new ConcurrentBag<string>();
            var tasks = stockTech.Select(i => Task.Run(() =>
                {
                    i.TechDataList = JsonConvert.DeserializeObject<List<StockTechData>>(i.TechData);
                    int techDataListCount = i.TechDataList.Count;
                    for (var j = 0; j < techDataListCount; j++)
                    {
                        i.TechDataList[j].MA5 = j + 5 <= techDataListCount ? i.TechDataList.Skip(j).Take(5).Average(x => x.Close) : 0;
                        i.TechDataList[j].MA10 = j + 10 <= techDataListCount ? i.TechDataList.Skip(j).Take(10).Average(x => x.Close) : 0;
                        i.TechDataList[j].MA20 = j + 20 <= techDataListCount ? i.TechDataList.Skip(j).Take(20).Average(x => x.Close) : 0;
                        i.TechDataList[j].MA60 = j + 60 <= techDataListCount ? i.TechDataList.Skip(j).Take(60).Average(x => x.Close) : 0;
                        i.TechDataList[j].MV5 = j + 5 <= techDataListCount ? (decimal)i.TechDataList.Skip(j).Take(5).Average(x => x.Volume) : 0;
                    }
                    if (!companyShareDict.TryGetValue(i.StockCode, out long issuedShares))
                    {
                        return;
                    }
                    i.TechDataList = i.TechDataList.OrderBy(x => x.Date).ToList();
                    #region

                    #endregion
                    #region 綠 K 周轉率 > 40%，隔天做空
                    for (int j = 3; j < techDataListCount; j++)
                    {
                        StockTechData today = i.TechDataList[j];
                        StockTechData yesterday = i.TechDataList[j - 1]; // 綠 K
                        StockTechData theDayBeforeYesterday = i.TechDataList[j - 2];
                        StockTechData theDayBeforeBeforeYesterday = i.TechDataList[j - 3];
                        decimal turnoverRate = (decimal)yesterday.Volume * 1000 / issuedShares;
                        decimal theoreticalLimitUp = yesterday.Close * 1.10m;
                        decimal finalTickSize = GetTickSize(theoreticalLimitUp);
                        decimal limitUpPrice = Math.Floor(theoreticalLimitUp / finalTickSize) * finalTickSize;
                        decimal previousTickSize = GetTickSize(limitUpPrice - 0.0001m);
                        decimal priceBeforeLimitUp = limitUpPrice - previousTickSize;
                        if (yesterday.Close < yesterday.Open && turnoverRate > 0.4m && theDayBeforeYesterday.Close / theDayBeforeBeforeYesterday.Close > 1.095m && theDayBeforeYesterday.Close == theDayBeforeYesterday.High && yesterday.Close < theDayBeforeYesterday.Close)
                        {
                            decimal returnRate = 0;
                            bool isWin = false;
                            if (today.High >= priceBeforeLimitUp)
                            {
                                returnRate = today.High / today.Open;
                                lock (_lockObject)
                                {
                                    totalReturnRate -= returnRate;
                                    loseCount++;
                                    reachLimitUpCount++;
                                }
                            }
                            else if (today.Close < today.Open)
                            {
                                returnRate = today.Open / today.Close;
                                lock (_lockObject)
                                {
                                    totalReturnRate += returnRate;
                                    winCount++;
                                }
                                isWin = true;
                            }
                            else
                            {
                                returnRate = today.Close / today.Open;
                                lock (_lockObject)
                                {
                                    totalReturnRate -= returnRate;
                                    loseCount++;
                                }
                            }
                            string winOrLose = isWin ? "WIN" : "LOSE";
                            string result = $"{i.StockCode},{today.Date.ToShortDateString()},{returnRate.ToString("0.00")},{yesterday.Volume},{issuedShares},{turnoverRate.ToString("0.00")},{today.Open},{today.Close},{winOrLose}";
                            results.Add(result);
                        }
                    }
                    #endregion
                    #region 大量上漲後，隔天小漲，再隔天開盤做多。
                    //for (var j = 0; j < techDataListCount - 3; j++)
                    //{
                    //    StockTechData today = i.TechDataList[j];
                    //    StockTechData yesterday = i.TechDataList[j + 1];
                    //    StockTechData theDayBeforeYesterday = i.TechDataList[j + 2]; // 紅 K
                    //    StockTechData theDayBeforeBeforeYesterday = i.TechDataList[j + 3];
                    //    if (theDayBeforeYesterday.Volume > theDayBeforeBeforeYesterday.MV5 * 7 && theDayBeforeYesterday.Volume > 10000 && theDayBeforeYesterday.Close > theDayBeforeBeforeYesterday.High && theDayBeforeYesterday.Close > theDayBeforeYesterday.Open && yesterday.Close > theDayBeforeYesterday.Close && yesterday.Close > yesterday.Open && yesterday.Close/theDayBeforeYesterday.Close < 1.02m)
                    //    {
                    //        for (var k = 0; k < techDataListCount; k++)
                    //        {
                    //            if (j - k < 0)
                    //            {
                    //                Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Holding");
                    //                break;
                    //            }
                    //            StockTechData currentTechData = i.TechDataList[j - k];
                    //            if (currentTechData.MA10 > yesterday.Low)
                    //            {
                    //                if (currentTechData.Close < currentTechData.MA10)
                    //                {
                    //                    if (currentTechData.Close > today.Open)
                    //                    {
                    //                        lock (_lockObject)
                    //                        {
                    //                            totalReturnRate += currentTechData.Close / today.Open;
                    //                            winCount++;
                    //                        }
                    //                        Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Sell Date: {currentTechData.Date.ToShortDateString()}, WIN, {(currentTechData.Close / today.Open).ToString("0.00")}%");
                    //                    }
                    //                    else
                    //                    {
                    //                        lock (_lockObject)
                    //                        {
                    //                            totalReturnRate -= today.Open / currentTechData.Close;
                    //                            loseCount++;
                    //                        }
                    //                        Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Sell Date: {currentTechData.Date.ToShortDateString()}, LOSE, {(today.Open / currentTechData.Close).ToString("0.00")}%");
                    //                    }
                    //                    break;
                    //                }
                    //            }
                    //            else
                    //            {
                    //                if (currentTechData.Close < yesterday.Low)
                    //                {
                    //                    if (currentTechData.Close > today.Open)
                    //                    {
                    //                        lock (_lockObject)
                    //                        {
                    //                            totalReturnRate += currentTechData.Close / today.Open;
                    //                            winCount++;
                    //                        }
                    //                        Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Sell Date: {currentTechData.Date.ToShortDateString()}, WIN, {(currentTechData.Close / today.Open).ToString("0.00")}%");
                    //                    }
                    //                    else
                    //                    {
                    //                        lock (_lockObject)
                    //                        {
                    //                            totalReturnRate -= today.Open / currentTechData.Close;
                    //                            loseCount++;
                    //                        }
                    //                        Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Sell Date: {currentTechData.Date.ToShortDateString()}, LOSE, {(today.Open / currentTechData.Close).ToString("0.00")}%");
                    //                    }
                    //                    break;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    #endregion
                    #region 多頭回檔，收盤站上所有均線買進，跌破五日均線出場
                    //for (var j = 0; j < techDataListCount - 11; j++)
                    //{

                    //    StockTechData today = i.TechDataList[j];
                    //    //StockTechData yesterday = i.TechDataList[j + 1];
                    //    //StockTechData theDayBeforeYesterday = i.TechDataList[j + 2];
                    //    List<StockTechData> prev10Days = i.TechDataList.Skip(j + 1).Take(10).ToList();
                    //    bool isAllAboveMA5 = prev10Days.All(x => x.Close > x.MA5 && x.MA5 != 0 && x.Close > x.MA60 && x.MA60 != 0);
                    //    if (today.Close < today.MA5 && isAllAboveMA5)
                    //    {
                    //        bool isHolding = false;
                    //        decimal buyPrice = 0;
                    //        for (int k = 1; k < techDataListCount; k++)
                    //        {
                    //            if (j - k < 0)
                    //            {
                    //                Console.WriteLine($"StockCode: {i.StockCode}, Date: {today.Date.ToShortDateString()}, Action: end");
                    //                break;
                    //            }
                    //            StockTechData currentTechData = i.TechDataList[j - k];
                    //            StockTechData prevTechData = i.TechDataList[j - k + 1];
                    //            if (isHolding)
                    //            {
                    //                //decimal exitPrice = currentTechData.MA5 > currentTechData.MA10 ? currentTechData.MA10 : currentTechData.MA5;
                    //                if (currentTechData.Close < currentTechData.MA5)
                    //                {
                    //                    bool isWin = currentTechData.Close > buyPrice ? true : false;
                    //                    decimal returnRate = isWin ? currentTechData.Close / buyPrice : buyPrice / currentTechData.Close;
                    //                    if (isWin)
                    //                    {
                    //                        lock (_lockObject)
                    //                        {
                    //                            totalReturnRate += returnRate;
                    //                            winCount++;
                    //                        }
                    //                        Console.WriteLine($"StockCode: {i.StockCode}, Date: {currentTechData.Date.ToShortDateString()}, Action: sell, WIN, {returnRate.ToString("0.00")}%");
                    //                        //writer.WriteLine($"{i.StockCode},{today.Date.ToShortDateString()},{currentTechData.Date.ToShortDateString()},WIN,{returnRate.ToString("0.00")}%");
                    //                    }
                    //                    else
                    //                    {
                    //                        lock (_lockObject)
                    //                        {
                    //                            totalReturnRate -= returnRate;
                    //                            loseCount++;
                    //                        }
                    //                        Console.WriteLine($"StockCode: {i.StockCode}, Date: {currentTechData.Date.ToShortDateString()}, Action: sell, LOSE, {returnRate.ToString("0.00")}%");
                    //                        //writer.WriteLine($"{i.StockCode},{today.Date.ToShortDateString()},{currentTechData.Date.ToShortDateString()},LOSE,{returnRate.ToString("0.00")}%");
                    //                    }
                    //                    isHolding = false;
                    //                    buyPrice = 0;
                    //                    if (currentTechData.Close < currentTechData.MA20)
                    //                    {
                    //                        Console.WriteLine($"StockCode: {i.StockCode}, Date: {currentTechData.Date.ToShortDateString()}, Action: removed");
                    //                        break;
                    //                    }
                    //                }
                    //            }
                    //            else
                    //            {
                    //                if (currentTechData.Close > currentTechData.MA5 && currentTechData.Close > currentTechData.MA10 && currentTechData.Close > currentTechData.MA20 && currentTechData.Close > currentTechData.MA60 && currentTechData.Volume > 10000 &&
                    //                    (prevTechData.Close <= prevTechData.MA5 || prevTechData.Close <= prevTechData.MA10 || prevTechData.Close <= prevTechData.MA20 || prevTechData.Close <= prevTechData.MA60))
                    //                {
                    //                    isHolding = true;
                    //                    buyPrice = currentTechData.Close;
                    //                    Console.WriteLine($"StockCode: {i.StockCode}, Date: {currentTechData.Date.ToShortDateString()}, Action: buy");
                    //                }
                    //                else if (currentTechData.Close < currentTechData.MA20)
                    //                {
                    //                    Console.WriteLine($"StockCode: {i.StockCode}, Date: {currentTechData.Date.ToShortDateString()}, Action: removed");
                    //                    break;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    #endregion
                    #region 漲停後，隔兩天都綠K，第三天開盤做空。
                    //for (int j = 0; j < techDataListCount - 4; j++)
                    //{
                    //    StockTechData today = i.TechDataList.Skip(j).First();
                    //    StockTechData yesterday = i.TechDataList.Skip(j + 1).First(); 
                    //    StockTechData theDayBeforeYesterday = i.TechDataList.Skip(j + 2).First();
                    //    StockTechData theDayBeforeBeforeYesterday = i.TechDataList.Skip(j + 3).First(); // 紅 K 
                    //    StockTechData theDayBeforeBeforeBeforeYesterday = i.TechDataList.Skip(j + 4).First();
                    //    if (theDayBeforeBeforeYesterday.Close / theDayBeforeBeforeBeforeYesterday.Close > 1.095m && theDayBeforeBeforeYesterday.Close == theDayBeforeBeforeYesterday.High && theDayBeforeYesterday.Close < theDayBeforeYesterday.Open && yesterday.Close < yesterday.Open && yesterday.Close < theDayBeforeYesterday.Close && yesterday.Volume < theDayBeforeYesterday.Volume && yesterday.Close < theDayBeforeBeforeYesterday.High)
                    //    {
                    //        if (today.Close < today.Open)
                    //        {
                    //            decimal returnRate = today.Open / today.Close;
                    //            lock (_lockObject)
                    //            {
                    //                totalReturnRate += returnRate;
                    //                winCount++;
                    //            }
                    //            Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Sell Date: {today.Date.ToShortDateString()}, WIN, {returnRate.ToString("0.00")}%");
                    //            writer.WriteLine($"{i.StockCode},{today.Date.ToShortDateString()},{today.Date.ToShortDateString()},WIN,{returnRate.ToString("0.00")}");
                    //        }
                    //        else
                    //        {
                    //            decimal returnRate = today.Close / today.Open;
                    //            lock (_lockObject)
                    //            {
                    //                totalReturnRate -= returnRate;
                    //                loseCount++;
                    //            }
                    //            Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Sell Date: {today.Date.ToShortDateString()}, LOSE, {returnRate.ToString("0.00")}%");
                    //            writer.WriteLine($"{i.StockCode},{today.Date.ToShortDateString()},{today.Date.ToShortDateString()},LOSE,{returnRate.ToString("0.00")}");
                    //        }
                    //    }
                    //}
                    #endregion
                    #region 大量上漲後隔天再漲，收盤前做多。 
                    //for (int j = 0; j < i.TechDataList.Count - 45; j++)
                    //{
                    //    StockTechData today = i.TechDataList.Skip(j).First();
                    //    StockTechData yesterday = i.TechDataList.Skip(j + 1).First(); // 紅 K 
                    //    StockTechData theDayBeforeYesterday = i.TechDataList.Skip(j + 2).First();
                    //    StockTechData theDayBeforeBeforeYesterday = i.TechDataList.Skip(j + 3).First();
                    //    var prevMV5 = i.TechDataList.Skip(j + 2).Take(5).Average(x => x.Volume);
                    //    if (yesterday.Volume > prevMV5 * 7 && yesterday.Volume > 10000 && yesterday.Close > theDayBeforeYesterday.Close &&
                    //        !(theDayBeforeYesterday.Close / theDayBeforeBeforeYesterday.Close > 1.095m && theDayBeforeYesterday.Close == theDayBeforeYesterday.High) && today.Close > yesterday.High && yesterday.Close > i.TechDataList.Skip(j + 2).Take(40).Max(x => x.High) && theDayBeforeYesterday.Close <= i.TechDataList.Skip(j + 3).Take(40).Max(x => x.High))
                    //    {
                    //        bool isRedBar = today.Close >= today.Open ? true : false;
                    //        bool isVolumeUp = today.Volume > yesterday.Volume ? true : false;
                    //        string increasement = (today.Close / yesterday.Close).ToString("0.00");
                    //        bool isLimitUp = yesterday.Close / theDayBeforeYesterday.Close > 1.095m && yesterday.Close == yesterday.High ? true : false;
                    //        for (int k = 1; k < i.TechDataList.Count - 40; k++)
                    //        {
                    //            if (j - k < 0)
                    //            {
                    //                Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Holding");
                    //                writer.WriteLine($"{i.StockCode},{today.Date.ToShortDateString()},,Holding,,{isRedBar},{isVolumeUp},{increasement},{isLimitUp}");
                    //                break;
                    //            }
                    //            StockTechData currentTeckData = i.TechDataList.Skip(j - k).First();
                    //            if (currentTeckData.Close < yesterday.High)
                    //            {
                    //                decimal returnRate = today.Close / currentTeckData.Close;
                    //                lock (_lockObject)
                    //                {
                    //                    totalReturnRate -= returnRate;
                    //                    loseCount++;
                    //                }
                    //                Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Sell Date: {currentTeckData.Date.ToShortDateString()}, LOSE {returnRate.ToString("0.00")}%");
                    //                writer.WriteLine($"{i.StockCode},{today.Date.ToShortDateString()},{currentTeckData.Date.ToShortDateString()},LOSE,{returnRate.ToString("0.00")},{isRedBar},{isVolumeUp},{increasement},{isLimitUp}");
                    //                break;
                    //            }
                    //            else
                    //            {
                    //                if (currentTeckData.Close < i.TechDataList.Skip(j - k).Take(10).Average(x => x.Close))
                    //                {
                    //                    if (currentTeckData.Close > today.Close)
                    //                    {
                    //                        decimal returnRate = currentTeckData.Close / today.Close;
                    //                        lock (_lockObject)
                    //                        {
                    //                            totalReturnRate += returnRate;
                    //                            winCount++;
                    //                        }
                    //                        Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Sell Date: {currentTeckData.Date.ToShortDateString()}, WIN {returnRate.ToString("0.00")}%");
                    //                        writer.WriteLine($"{i.StockCode},{today.Date.ToShortDateString()},{currentTeckData.Date.ToShortDateString()},WIN,{returnRate.ToString("0.00")},{isRedBar},{isVolumeUp},{increasement},{isLimitUp}");
                    //                        break;
                    //                    }
                    //                    else
                    //                    {
                    //                        decimal returnRate = today.Close / currentTeckData.Close;
                    //                        lock (_lockObject)
                    //                        {
                    //                            totalReturnRate -= returnRate;
                    //                            loseCount++;
                    //                        }
                    //                        Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Sell Date: {currentTeckData.Date.ToShortDateString()}, LOSE {returnRate.ToString("0.00")}%");
                    //                        writer.WriteLine($"{i.StockCode},{today.Date.ToShortDateString()},{currentTeckData.Date.ToShortDateString()},LOSE,{returnRate.ToString("0.00")},{isRedBar},{isVolumeUp},{increasement},{isLimitUp}");
                    //                        break;
                    //                    }
                    //                }
                    //            }
                    //        }

                    //    }
                    //}
                    #endregion
                    #region 紅 K 漲停後隔天做空
                    //for (int j = 0; j < i.TechDataList.Count - 2; j++)
                    //{
                    //    StockTechData today = i.TechDataList[j];
                    //    StockTechData yesterday = i.TechDataList[j + 1]; // 紅 K 漲停
                    //    StockTechData theDayBeforeYesterday = i.TechDataList[j + 2];

                    //    decimal theoreticalLimitUp = theDayBeforeYesterday.Close * 1.10m;
                    //    decimal finalTickSize = GetTickSize(theoreticalLimitUp);
                    //    decimal limitUpPrice = Math.Floor(theoreticalLimitUp / finalTickSize) * finalTickSize;
                    //    decimal turnoverRate = (decimal)yesterday.Volume * 1000 / issuedShares;
                    //    if (yesterday.Close == limitUpPrice && yesterday.Open < yesterday.Close && turnoverRate > 0.2m)
                    //    {
                    //        decimal todayTheoreticalLimitUp = yesterday.Close * 1.10m;
                    //        decimal todayFinalTickSize = GetTickSize(todayTheoreticalLimitUp);
                    //        decimal todayLimitUpPrice = Math.Floor(todayTheoreticalLimitUp / todayFinalTickSize) * todayFinalTickSize;
                    //        decimal previousTickSize = GetTickSize(todayLimitUpPrice - 0.0001m);
                    //        decimal todayPriceBeforeLimitUp = todayLimitUpPrice - previousTickSize;
                    //        bool isWin = today.Open > today.Close ? true : false;
                    //        decimal returnRate = isWin ? today.Open / today.Close : today.Close / today.Open;
                    //        string result = $"{i.StockCode},{today.Date.ToShortDateString()},{returnRate.ToString("0.00")},{yesterday.Volume},{issuedShares},{turnoverRate.ToString("0.00")},{today.Open},{today.Close},{(yesterday.Close / yesterday.Open).ToString("0.00")}";
                    //        if (today.High >= todayPriceBeforeLimitUp)
                    //        {
                    //            returnRate = today.High / today.Open;
                    //            lock (_lockObject)
                    //            {
                    //                totalReturnRate -= returnRate;
                    //                loseCount++;
                    //            }
                    //            result += ",LOSE";
                    //            reachLimitUpCount++;
                    //            Console.WriteLine($"StockCode: {i.StockCode}, Buy Date: {today.Date.ToShortDateString()}, ReachLimitUp");
                    //        }
                    //        else if (isWin)
                    //        {
                    //            lock (_lockObject)
                    //            {
                    //                totalReturnRate += returnRate;
                    //                winCount++;
                    //            }
                    //            result += ",WIN";
                    //        }
                    //        else
                    //        {
                    //            lock (_lockObject)
                    //            {
                    //                totalReturnRate -= returnRate;
                    //                loseCount++;
                    //            }
                    //            result += ",LOSE";
                    //        }
                    //        Console.WriteLine(result);
                    //        results.Add(result);
                    //    }
                    //}
                    #endregion
                }));
            await Task.WhenAll(tasks);
            #region 每天從漲停股選出周轉率最高的一檔，且周轉率大於 20%，隔天做空,若高點碰到漲停前一檔停損
            //List<DateTime> tradingDayList = stockTech.First().TechDataList.Select(x => x.Date).ToList();
            //for (int i = 1; i < tradingDayList.Count; i++)
            //{
            //    DateTime todayDate = tradingDayList[i];
            //    DateTime yesterdayDate = tradingDayList[i - 1];
            //    //DateTime theDayBeforeYesterdayDate = tradingDayList[i - 2];
            //    //List<(string, int)> limitUpList = new List<(string, int)>();
            //    List<(string, decimal)> turnoverRateList = new List<(string, decimal)>();
            //    foreach (var j in stockTech)
            //    {
            //        var today = j.TechDataList.FirstOrDefault(x => x.Date == todayDate);
            //        var yesterday = j.TechDataList.FirstOrDefault(x => x.Date == yesterdayDate);
            //        //var theDayBeforeYesterday = j.TechDataList.FirstOrDefault(x => x.Date == theDayBeforeYesterdayDate);
            //        if (today == null || yesterday == null) continue;
            //        if (companyShareDict.TryGetValue(j.StockCode, out long issuedShares))
            //        {
            //            decimal turnoverRate = (decimal)yesterday.Volume * 1000 / issuedShares;
            //            if (turnoverRate > 0.4m)
            //            {
            //                turnoverRateList.Add((j.StockCode, turnoverRate));
            //            }
                            
            //        }
                    
            //        //decimal theoreticalLimitUp = theDayBeforeYesterday.Close * 1.10m;
            //        //decimal finalTickSize = GetTickSize(theoreticalLimitUp);
            //        //decimal limitUpPrice = Math.Floor(theoreticalLimitUp / finalTickSize) * finalTickSize;
            //        //if (yesterday.Close == limitUpPrice)
            //        //{
            //        //    limitUpList.Add((j.StockCode, yesterday.Volume));
            //        //}
            //    }
                
            //    //foreach (var j in limitUpList)
            //    //{
            //    //    if (companyShareDict.TryGetValue(j.Item1, out long issuedShares))
            //    //    {

            //    //        turnoverRateList.Add((j.Item1, (decimal)j.Item2 * 1000 / issuedShares));
            //    //    }
            //    //}
            //    var highestTurnoverRateStock = turnoverRateList.OrderByDescending(x => x.Item2).FirstOrDefault();
            //    if (!string.IsNullOrEmpty(highestTurnoverRateStock.Item1))
            //    {
            //        StockTech stock = stockTech.First(x => x.StockCode == highestTurnoverRateStock.Item1);

            //        var today = stock.TechDataList.First(x => x.Date == todayDate);
            //        var yesterday = stock.TechDataList.First(x => x.Date == yesterdayDate);
            //        //var theDayBeforeYesterday = stock.TechDataList.First(x => x.Date == theDayBeforeYesterdayDate);
            //        decimal returnRate = 0;
            //        decimal theoreticalLimitUp = yesterday.Close * 1.10m;
            //        decimal finalTickSize = GetTickSize(theoreticalLimitUp);
            //        decimal limitUpPrice = Math.Floor(theoreticalLimitUp / finalTickSize) * finalTickSize;
            //        decimal previousTickSize = GetTickSize(limitUpPrice - 0.0001m);
            //        decimal priceBeforeLimitUp = limitUpPrice - previousTickSize;
            //        bool isWin = false;
            //        if (today.High >= priceBeforeLimitUp)
            //        {
            //            returnRate = today.High / today.Open;
            //            totalReturnRate -= returnRate;
            //            loseCount++;
            //            reachLimitUpCount++;
            //            Console.WriteLine($"StockCode: {stock.StockCode}, Buy Date: {today.Date.ToShortDateString()}, Sell Date: {today.Date.ToShortDateString()}, LOSE, {returnRate.ToString("0.00")}%, TurnoverRate: {highestTurnoverRateStock.Item2.ToString("0.00")}");
            //        }
            //        else if (today.Close < today.Open)
            //        {
            //            returnRate = today.Open / today.Close;
            //            if (returnRate < 1.015m)
            //            {
            //                loseCount++;
            //            }
            //            else
            //            {
            //                totalReturnRate += returnRate;
            //                winCount++;
            //                isWin = true;
            //            }
                            
            //        }
            //        else
            //        {
            //            returnRate = today.Close / today.Open;
            //            totalReturnRate -= returnRate;
            //            loseCount++;
            //        }
            //        companyShareDict.TryGetValue(stock.StockCode, out long issuedShares);
            //        string result = $"{stock.StockCode},{today.Date.ToShortDateString()},{returnRate.ToString("0.00")},{yesterday.Volume},{issuedShares},{highestTurnoverRateStock.Item2.ToString("0.00")},{today.Open},{today.Close},{(yesterday.Close / yesterday.Open).ToString("0.00")}";
            //        result += isWin ? ",WIN" : ",LOSE";
            //        results.Add(result);
            //    }
            //}
            #endregion

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("StockCode,BuyDate,ReturnRate,漲停當日成交量,發行股數,漲停當日周轉率,開盤價,收盤價,Result");
                foreach (var i in results)
                {
                    writer.WriteLine(i);
                }
            }
            Console.WriteLine($"Win: {winCount}, Lose: {loseCount}, Total Return Rate: {totalReturnRate.ToString("0.00")}%, ReachLimitUpCount: {reachLimitUpCount}");

        }
        private static decimal GetTickSize(decimal price)
        {
            if (price < 10m)
                return 0.01m;
            if (price < 50m)
                return 0.05m;
            if (price < 100m)
                return 0.1m;
            if (price < 500m)
                return 0.5m;
            if (price < 1000m)
                return 1.0m;

            return 5.0m;
        }

        public class ApiResponse
        {
            public Data Data { get; set; }
            public int Status { get; set; }
        }

        public class Data
        {
            public List<MainDetail> MainInDetails { get; set; }
            public List<MainDetail> MainOutDetails { get; set; }
            public int Summary { get; set; }
            public string UpdateDate { get; set; }
        }

        public class MainDetail
        {
            public string SecuritiesCompName { get; set; }
            public string SecuritiesCompSymbol { get; set; }
            public double Count { get; set; }
            public double Price { get; set; }
            public double BuyCount { get; set; }
            public double SellCount { get; set; }
            public double TradeRatio { get; set; }
        }

        public static async Task<ApiResponse> FetchMainInOutDetailsAsync(string symbol, int dayRange, HttpClient client)
        {
            Console.WriteLine($"Fetch {symbol} started");
            string url = $"https://ytdf.yuanta.com.tw/prod/yesidmz/api/chipanalysis/maininoutdetail?symbol={symbol}&dayRange={dayRange}";
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var a = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
                Console.WriteLine($"Fetch {symbol} finished");
                return a;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data for {symbol}: {ex.Message}");
                return null;
            }
        }
    }
}
