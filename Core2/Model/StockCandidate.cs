using System;
using System.Collections.Generic;
using System.Text;

namespace Core2.Model
{
    public class StockCandidate : StockSharedModel
    {
        public List<StockTechData> TechDataList { get; set; } = new List<StockTechData>();
        public bool IsCandidate { get; set; }
        public decimal GapUpHigh { get; set; }
        public decimal GapUpLow { get; set; }
        public DateTime SelectedDate { get; set; }
        public decimal EntryPoint { get; set; }
        public DateTime? ExRrightsExDividendDateTime { get; set; }
        public decimal LimitUpPrice { get; set; }
        public decimal PriceBeforeLimitUp { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal LimitDownPrice { get; set; }
        public decimal TurnoverRate { get; set; }
    }
}
