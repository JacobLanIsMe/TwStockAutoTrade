using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Core2.Service;

namespace Selector2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location))
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build())
                .AddLogging(builder => builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information))
                .AddSingleton<DiscordService>()                 // required by StockSelectorService
                .AddSingleton<StockSelectorService>()           // register the concrete service
                .BuildServiceProvider();

            var stockSelector = serviceProvider.GetRequiredService<StockSelectorService>();

            try
            {
                Task.Run(async () => await stockSelector.SelectStock()).Wait();
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILogger<Program>>();
                logger?.LogError(ex, "Unhandled exception running SelectStock");
                Console.WriteLine(ex);
            }
        }
    }
}
