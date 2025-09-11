using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class StockTechData
    {
        public DateTime Date { get; set; }
        public decimal Close  { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public int Volume { get; set; }
        public decimal MA5 { get; set; }
        public decimal MA10 { get; set; }
        public decimal MA20 { get; set; }
        public decimal MA60 { get; set; }
        public decimal MV5 { get; set; }
    }
}
