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
                    todayTechData.Volume > 1000 && todayTechData.Volume > mv5 * 3 &&
                    ma5 > prevMa5 && ma10 > prevMa10 && ma20 > prevMa20 && ma60 > prevMa60)
                {
                    newCandidates.Add(i);
                }
            }
        }
    }
}
