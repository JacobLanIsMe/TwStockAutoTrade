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

namespace FutureTrader
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("C://Logs/Trader/log-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 250)
            .CreateLogger();
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location))
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build())
                .AddSingleton<ILogger>(Log.Logger)
                .AddSingleton<IFutureTraderService, FutureTraderService>()
                .AddSingleton<IYuantaService, YuantaService>()
                .BuildServiceProvider();
            var futureTraderService = serviceProvider.GetRequiredService<IFutureTraderService>();
            Task.Run(async () => await futureTraderService.Trade()).Wait();
        }
    }
}
