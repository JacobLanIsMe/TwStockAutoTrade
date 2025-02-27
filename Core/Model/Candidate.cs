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
        public int StockCode { get; set; }
        public string CompanyName { get; set; }
        public List<StockTechData> TechDataList { get; set; } = new List<StockTechData>();
        public List<StockTechData> OrderedTechDataList { get { return TechDataList.OrderByDescending(x => x.Date).ToList(); } }
        public decimal? GapUpHigh 
        {
            get 
            {
                StockTechData gapUpStockTechData = GetGapUpStockTechData();
                if (gapUpStockTechData == null) return GapUpHigh;
                return gapUpStockTechData.High;
            }
            set
            {
                GapUpHigh = value;
            }
        }
        public decimal? GapUpLow
        {
            get
            {
                StockTechData gapUpStockTechData = GetGapUpStockTechData();
                if (gapUpStockTechData == null) return GapUpLow;
                return gapUpStockTechData.Low;
            }
            set
            {
                GapUpLow = value;
            }
        }
        public decimal? StopLossPoint 
        {
            get
            {
                if (GapUpHigh == null)
                {
                    return null;
                }
                else if (GapUpHigh <= 10)
                {
                    return GapUpHigh - (decimal)(0.01 * 2);
                }
                else if (GapUpHigh == (decimal)10.05)
                {
                    return (decimal)9.99;
                }
                else if (GapUpHigh > (decimal)10.05 & GapUpHigh <= 50)
                {
                    return GapUpHigh - (decimal)(0.05 * 2);
                }
                else if (GapUpHigh == (decimal)50.1)
                {
                    return (decimal)49.95;
                }
                else if (GapUpHigh > (decimal)50.1 & GapUpHigh <= 100)
                {
                    return GapUpHigh - (decimal)(0.1 * 2);
                }
                else if (GapUpHigh == (decimal)100.5)
                {
                    return (decimal)99.9;
                }
                else if (GapUpHigh > (decimal)100.5 & GapUpHigh <= 500)
                {
                    return GapUpHigh - (decimal)(0.5 * 2);
                }
                else if (GapUpHigh == (decimal)501)
                {
                    return (decimal)499.5;
                }
                else if (GapUpHigh > 501 & GapUpHigh <= 1000)
                {
                    return GapUpHigh - (1 * 2);
                }
                else if (GapUpHigh == 1005)
                {
                    return 999;
                }
                else
                {
                    return GapUpHigh - (5 * 2);
                }
            } 
        }
        public bool IsCandidate 
        { 
            get
            {
                if (OrderedTechDataList.Count < 25) return false;
                StockTechData gapUpStockTechData = GetGapUpStockTechData();
                if (gapUpStockTechData.Low <= OrderedTechDataList[5].High) return false;
                double mv5 = OrderedTechDataList.Take(5).Average(x => x.Volume);
                if (mv5 < 100) return false;
                decimal volatility = OrderedTechDataList.Take(5).Max(x => x.Close) / OrderedTechDataList.Take(5).Min(x => x.Close);
                if (volatility > (decimal)1.02) return true;
                decimal gapUpMa5 = OrderedTechDataList.Skip(4).Take(5).Average(x => x.Close);
                decimal gapUpMa10 = OrderedTechDataList.Skip(4).Take(10).Average(x => x.Close);
                decimal gapUpMa20 = OrderedTechDataList.Skip(4).Take(20).Average(x => x.Close);
                if (gapUpStockTechData.Close < gapUpMa5 || gapUpStockTechData.Close < gapUpMa10 || gapUpStockTechData.Close < gapUpMa20) return false;
                List<decimal> last4Close = OrderedTechDataList.Take(4).Select(x => x.Close).ToList();
                bool isPeriodCloseHigherThanGapUpHigh = last4Close.Max() > gapUpStockTechData.High;
                bool isPeriodCloseLowerThanGapUpLow = last4Close.Min() < gapUpStockTechData.Low;
                if (isPeriodCloseHigherThanGapUpHigh || isPeriodCloseLowerThanGapUpLow) return false;
                return true;
            } 
        }
        private StockTechData GetGapUpStockTechData()
        {
            if (OrderedTechDataList.Count < 5) return null;
            return OrderedTechDataList[4];
        }
        public DateTime? SelectDate
        {
            get
            {
                if (OrderedTechDataList.Any()) return OrderedTechDataList.First().Date;
                return SelectDate;
            }
            set
            {
                SelectDate = value;
            }
        }
    }
}
