using Core.Repository;
using Core.Repository.Interface;
using Core.Service;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortStock
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("C://Logs/StockTrader/log-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 250)
            .CreateLogger();
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location))
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build())
                .AddSingleton<ILogger>(Log.Logger)
                .AddSingleton<ICandidateForShortRepository, CandidateForShortRepository>()
                .AddSingleton<IShortStockService, ShortStockService>()
                .AddSingleton<IYuantaService, YuantaService>()
                .AddSingleton<IDiscordService, DiscordService>()
                .AddSingleton<IDateTimeService, DateTimeService>()
                .BuildServiceProvider();
            var shortStockService = serviceProvider.GetRequiredService<IShortStockService>();
            try
            {
                Task.Run(async () => await shortStockService.Trade()).Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
