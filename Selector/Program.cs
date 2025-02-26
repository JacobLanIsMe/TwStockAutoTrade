using Core.Interface;
using Core.Service;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
namespace Selector
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
             .AddSingleton<IGetStockInfoService, GetStockInfoService>()
             .AddSingleton<IDateTimeService, DateTimeService>()
             .BuildServiceProvider();
            var getStockInfoService = serviceProvider.GetRequiredService<IGetStockInfoService>();
            Task.Run(async () => await getStockInfoService.SelectStock()).Wait();
        }
    }
}
