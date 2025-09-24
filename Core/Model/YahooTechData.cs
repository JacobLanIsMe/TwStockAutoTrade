using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class YahooTechData
    {
        public List<DataModel> Data { get; set; }
        //public int T { get;set; }
        //public decimal O { get; set; }
        //public decimal H { get; set; }
        //public decimal L { get; set; }
        //public decimal C { get; set; }
        //public int V { get; set; }
    }
    public class DataModel
    {
        public string Symbol { get; set; }
        public ChartModel Chart { get; set; }
    }
    public class ChartModel
    {
        public List<long> Timestamp { get; set; }
        public IndicatorModel Indicators { get; set; }
    }
    public class IndicatorModel
    {
        public List<QuoteModel> Quote { get; set; }
    }
    public class QuoteModel
    {
        public List<decimal> Close { get; set; }
        public List<decimal> High { get; set; }
        public List<decimal> Low { get; set; }
        public List<decimal> Open { get; set; }
        public List<int> Volume { get; set; }
    }
}
