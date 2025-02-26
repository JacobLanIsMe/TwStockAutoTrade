using Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class Stock
    {
        public EMarket Market { get; set; }
        public int StockCode { get; set; }
        public string CompanyName { get; set; }

    }
}
