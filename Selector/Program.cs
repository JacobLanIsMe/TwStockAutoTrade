using Core.Repository;
using Core.Repository.Interface;
using Core.Service;
using Core.Service.Interface;
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
             .AddSingleton<ICandidateRepository, CandidateRepository>()
             .BuildServiceProvider();
            var getStockInfoService = serviceProvider.GetRequiredService<IGetStockInfoService>();
            Task.Run(async () => await getStockInfoService.SelectStock()).Wait();
        }
    }
}
