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
        public int PurchasedLot { get; set; }
        public decimal SumOfLast9Close { get; set; }
        public bool IsTradingStarted { get; set; }
        public long IssuedShare { get; set; }
        public bool IsOrdered { get; set; }
        public int OrderedLot { get; set; }
    }
}
