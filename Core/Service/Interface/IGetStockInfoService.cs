using System.Threading.Tasks;

namespace Core.Service.Interface
{
    public interface IGetStockInfoService
    {
        Task SelectStock();
    }
}
