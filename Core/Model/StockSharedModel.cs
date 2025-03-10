using Core.Enum;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Model
{
    public class StockSharedModel
    {
        public Guid Id { get; set; }
        public enumMarketType Market { get; set; }
        public string StockCode { get; set; }
        public string CompanyName { get; set; }
        public string Last9TechData { get; set; }
        public decimal StopLossPoint { get; set; }
        public List<decimal> Last9Close
        {
            get
            {
                if (string.IsNullOrEmpty(Last9TechData))
                {
                    return new List<decimal>();
                }
                List<StockTechData> last9TechDataList = JsonConvert.DeserializeObject<List<StockTechData>>(Last9TechData);
                return last9TechDataList.Select(x => x.Close).ToList();
            }
        }
    }
}
