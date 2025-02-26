using System.Threading.Tasks;

namespace Core.Interface
{
    public interface IGetStockInfoService
    {
        Task SelectStock();
    }
}
