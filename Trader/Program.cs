using Core.Repository.Interface;
using Core.Repository;
using Core.Service;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Trader
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
                .AddSingleton<IStockTraderService, StockTraderService>()
                .AddSingleton<IDateTimeService, DateTimeService>()
                .AddSingleton<ICandidateRepository, CandidateRepository>()
                .AddSingleton<ITradeRepository, TradeRepository>()
                .AddSingleton<IYuantaService, YuantaService>()
                .BuildServiceProvider();
            var stockTraderService = serviceProvider.GetRequiredService<IStockTraderService>();
            try
            {
                Task.Run(async () => await stockTraderService.Trade()).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}
