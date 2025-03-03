using Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class StockSharedModel
    {
        public Guid Id { get; set; }
        public EMarket Market { get; set; }
        public string StockCode { get; set; }
        public string CompanyName { get; set; }
        public string Last9TechData { get; set; }
        public decimal StopLossPoint { get; set; }
    }
}
