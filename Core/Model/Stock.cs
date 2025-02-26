using Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class Stock
    {
        public EMarket Market { get; set; }
        public int StockCode { get; set; }
        public string CompanyName { get; set; }
        public List<StockTechData> TechDataList { get; set; } = new List<StockTechData>();
        private List<StockTechData> OrderedTechDataList { get { return TechDataList.OrderByDescending(x => x.Date).ToList(); } }
        public decimal? EntryPoint 
        {
            get 
            {
                StockTechData gapUpStockTechData = GetGapUpStockTechData();
                return gapUpStockTechData?.High;
            }
        }
        public decimal? StopLossPoint 
        {
            get
            {
                if (EntryPoint == null)
                {
                    return null;
                }
                else if (EntryPoint <= 10)
                {
                    return EntryPoint - (decimal)(0.01 * 2);
                }
                else if (EntryPoint == (decimal)10.05)
                {
                    return (decimal)9.99;
                }
                else if (EntryPoint > (decimal)10.05 & EntryPoint <= 50)
                {
                    return EntryPoint - (decimal)(0.05 * 2);
                }
                else if (EntryPoint == (decimal)50.1)
                {
                    return (decimal)49.95;
                }
                else if (EntryPoint > (decimal)50.1 & EntryPoint <= 100)
                {
                    return EntryPoint - (decimal)(0.1 * 2);
                }
                else if (EntryPoint == (decimal)100.5)
                {
                    return (decimal)99.9;
                }
                else if (EntryPoint > (decimal)100.5 & EntryPoint <= 500)
                {
                    return EntryPoint - (decimal)(0.5 * 2);
                }
                else if (EntryPoint == (decimal)501)
                {
                    return (decimal)499.5;
                }
                else if (EntryPoint > 501 & EntryPoint <= 1000)
                {
                    return EntryPoint - (1 * 2);
                }
                else if (EntryPoint == 1005)
                {
                    return 999;
                }
                else
                {
                    return EntryPoint - (5 * 2);
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
    }
}
