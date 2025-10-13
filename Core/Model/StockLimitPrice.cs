using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class StockLimitPrice
    {
        public decimal LimitUpPrice { get; set; }
        public decimal PriceBeforeLimitUp { get; set; }
        public decimal LimitDownPrice { get; set; }
    }
}
