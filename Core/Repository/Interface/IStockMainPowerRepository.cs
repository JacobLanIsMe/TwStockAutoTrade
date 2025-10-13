using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repository.Interface
{
    public interface IStockMainPowerRepository
    {
        Task Insert(List<StockMainPower> stockMainPowerList);
        Task<List<StockMainPower>> GetRecordsWithNullTomorrowTechData();
        Task Update(List<StockMainPower> stockMainPowerList);
    }
}
