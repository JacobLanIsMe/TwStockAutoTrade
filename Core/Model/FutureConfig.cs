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
        public string FutureCode { get; set; }
        public string CommodityId { get; set; }
        public string TaifexCode { get; set; }
        public EPeriod Period { get; set; }
        public TimeSpan MarketOpenTime { get; set; }
        public TimeSpan MarketCloseTime { get; set; }
    }
}
