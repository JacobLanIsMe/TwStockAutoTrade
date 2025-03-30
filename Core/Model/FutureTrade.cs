using Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class FutureTrade
    {
        public string OrderNo { get; set; } = string.Empty;
        public EFutureOrderType OrderType { get; set; }
        public EBuySellType BuySell { get; set; }
        public int Point { get; set; }
        //public DateTime? TradeDateTime { get; set; }
        //public bool IsCancelled { get; set; }
    }
}
