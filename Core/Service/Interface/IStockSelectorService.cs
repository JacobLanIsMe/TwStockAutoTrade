using Core.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Service.Interface
{
    public interface IStockSelectorService
    {
        Task SelectStock();
        Task<List<StockCandidate>> SelectCrazyStock();
    }
}
