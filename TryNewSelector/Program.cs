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

            //string url_wantgoo = "https://www.wantgoo.com/stock/2330/major-investors/main-trend-data";
            //string url_
            //SimpleHttpClientFactory simpleHttpClientFactory = new SimpleHttpClientFactory();
            //HttpClient httpClient = simpleHttpClientFactory.CreateClient();
            //var html = await httpClient.GetStringAsync(url_wantgoo);

            // 解析 HTML
            //var doc = new HtmlDocument();
            //doc.LoadHtml(html);


            var candidateRepository = serviceProvider.GetRequiredService<ICandidateRepository>();
            List<StockTech> stockTech = await candidateRepository.GetStockTech();
            List<StockTech> newCandidates = new List<StockTech>();
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
