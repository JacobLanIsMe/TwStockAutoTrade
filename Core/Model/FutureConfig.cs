using Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class FutureConfig
    {
        public string TaifexFutureCode { get; set; }
        public string YuantaFutureCode { get; set; }
        public string CommodityId { get; set; }
        public TimeSpan TimeThreshold { get; set; }
        public TimeSpan MarketOpenTime { get; set; }
        public TimeSpan MarketCloseTime { get; set; }
    }
}
