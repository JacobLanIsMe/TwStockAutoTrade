﻿using Core.Enum;
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
        public EBuySellType BuySell { get; set; }
        public int OrderedLot { get; set; }
        public int PurchasedLot { get; set; }
        public int Point { get; set; }
        public EOpenOffsetKind OpenOffsetKind { get; set; }
    }
}
