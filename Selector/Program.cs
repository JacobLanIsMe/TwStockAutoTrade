using Core.Repository;
using Core.Repository.Interface;
using Core.Service;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;
namespace Selector
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location))
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build())
                .AddSingleton<IGetStockInfoService, GetStockInfoService>()
                .AddSingleton<IDateTimeService, DateTimeService>()
                .AddSingleton<ICandidateRepository, CandidateRepository>()
                .AddSingleton<ICandidateRepository, CandidateRepository>()
                .BuildServiceProvider();
            var getStockInfoService = serviceProvider.GetRequiredService<IGetStockInfoService>();
            Task.Run(async () => await getStockInfoService.SelectStock()).Wait();
        }
    }
}
