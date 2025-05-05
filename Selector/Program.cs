using Core.Repository;
using Core.Repository.Interface;
using Core.Service;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
namespace Selector
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("C://Logs/StockSelector/log-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 250)
            .CreateLogger();
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location))
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build())
                .AddSingleton<ILogger>(Log.Logger)
                .AddSingleton<IStockSelectorService, StockSelectorService>()
                .AddSingleton<IDateTimeService, DateTimeService>()
                .AddSingleton<ICandidateRepository, CandidateRepository>()
                .AddSingleton<ITradeRepository, TradeRepository>()
                .AddSingleton<IDiscordService, DiscordService>()
                .BuildServiceProvider();
            var getStockInfoService = serviceProvider.GetRequiredService<IStockSelectorService>();
            try
            {
                Task.Run(async () => await getStockInfoService.SelectStock()).Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
