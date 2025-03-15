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
        public EPeriod Period { get; set; }
        public TimeSpan MarketOpenTime { get; set; }
        public TimeSpan MarketCloseTime { get; set; }
        public TimeSpan LastEntryTime
        {
            get
            {
                return MarketCloseTime.Subtract(TimeSpan.FromHours(2));
            }
        }
    }
}
