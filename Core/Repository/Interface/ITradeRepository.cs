using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repository.Interface
{
    public interface ITradeRepository
    {
        Task<List<Trade>> GetStockHolding();
        Task UpdateLast9TechData(List<Trade> tradeList);
    }
}
