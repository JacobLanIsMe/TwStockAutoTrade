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
        public decimal Ma5 { get; set; }
        public decimal Ma10 { get; set; }
        public decimal Ma20 { get; set; }
        public decimal Ma60 { get; set; }
        public decimal Mv5 { get; set; }
    }
}
