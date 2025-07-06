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

            List<StockTech> newCandidates = new List<StockTech>();
            foreach (var i in stockTech)
            {

                string url = $"https://stockchannelnew.sinotrade.com.tw/Z/ZC/ZCO/CZCO.DJBCD?A={i.StockCode}";
                HttpResponseMessage response = await httpClient.GetAsync(url);
                string responseBody = await response.Content.ReadAsStringAsync();
                var parts = responseBody.Split(' ');
                string[] mainPowerArray = parts[2].Split(',');
                List<string> mainPowerList = mainPowerArray.Skip(mainPowerArray.Length - 5).ToList();
                int count = 0;
                for (var j = 1; j <=5; j++)
                {
                    if (int.TryParse(mainPowerArray[mainPowerArray.Length - j], out int mainPower) && mainPower > 0) count++;
                }

                if (count < 4) continue;
                
                //Console.WriteLine($"{i.StockCode} match main power");
                if (i.TechDataList.Count < 5) continue;

                decimal todayClose = i.TechDataList.First().Close;
                decimal high = i.TechDataList.Take(5).Max(x => x.Close);
                decimal low = i.TechDataList.Take(5).Min(x => x.Close);
                decimal ma60 = i.TechDataList.Take(60).Average(x => x.Close);
                decimal mv5 = (decimal)i.TechDataList.Take(5).Average(x => x.Volume);

                if (high / low > 1.02m || mv5 < 1000 || todayClose < ma60) continue;
                //Console.WriteLine($"{i.StockCode} match the volume filter");
                    
                List<StockTechData> latest5TechList = i.TechDataList.Take(5).ToList();
                int isSideWay = 0;
                for (int j = 0; j < latest5TechList.Count; j++)
                {
                    decimal baseHigh = latest5TechList[j].High;
                    decimal baseLow = latest5TechList[j].Low;
                    for (int k = 0; k < latest5TechList.Count; k++) 
                    { 
                        if(j==k) continue;
                        if (baseHigh >= latest5TechList[k].Low && latest5TechList[k].High >= baseLow)
                        {
                            isSideWay++;
                        }
                    }

                }
                if (isSideWay < 20) continue;
                Console.WriteLine($"{i.StockCode} match all filter");
            }

            foreach (var i in stockTech)
            {
                if (i.TechDataList.Count() < 100) continue;
                bool isFirstDayVolumeSpike = true;
                for (int j = 1; j < 5; j++)
                {
                    if (i.TechDataList.Skip(j).First().Volume > i.TechDataList.Skip(j).Take(5).Average(x => x.Volume) * 3)
                    {
                        isFirstDayVolumeSpike = false;
                        break;
                    }
                }
                if (!isFirstDayVolumeSpike) continue;
                decimal ma5 = i.TechDataList.Take(5).Average(x => x.Close);
                decimal ma10 = i.TechDataList.Take(10).Average(x => x.Close);
                decimal ma20 = i.TechDataList.Take(20).Average(x => x.Close);
                decimal ma60 = i.TechDataList.Take(60).Average(x => x.Close);
                decimal prevMa5 = i.TechDataList.Skip(1).Take(5).Average(x => x.Close);
                decimal prevMa10 = i.TechDataList.Skip(1).Take(10).Average(x => x.Close);
                decimal prevMa20 = i.TechDataList.Skip(1).Take(20).Average(x => x.Close);
                decimal prevMa60 = i.TechDataList.Skip(1).Take(60).Average(x => x.Close);
                decimal mv5 = (decimal)i.TechDataList.Take(5).Average(x => x.Volume);
                var todayTechData = i.TechDataList.First();

                if (todayTechData.Close > ma5 && todayTechData.Close > ma10 && todayTechData.Close > ma20 && todayTechData.Close > ma60 &&
                    todayTechData.Volume > 1000 && todayTechData.Volume > mv5 * 3)
                    //ma5 > prevMa5 && ma10 > prevMa10 && ma20 > prevMa20 && ma60 > prevMa60)
                {
                    newCandidates.Add(i);
                }
                Console.WriteLine($"{i.StockCode} {i.CompanyName} finished.");
            }
            Console.WriteLine("-------------------------------");
            foreach (var i in newCandidates)
            {
                Console.WriteLine($"{i.StockCode} match the tech filter");
            }
        }
    }
}
