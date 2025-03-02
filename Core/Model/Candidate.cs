using Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class Candidate
    {
        public Guid Id { get; set; }
        public EMarket Market { get; set; }
        public string StockCode { get; set; }
        public string CompanyName { get; set; }
        public List<StockTechData> TechDataList { get; set; } = new List<StockTechData>();
        public bool IsCandidate { get; set; }
        public decimal? GapUpHigh { get; set; }
        public decimal? GapUpLow { get; set; }
        public decimal? StopLossPoint { get; set; }
        public DateTime? SelectedDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeleteDate { get; set; }
        public string Last9TechData { get; set; }
    }
}
