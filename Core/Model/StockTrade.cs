using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class StockTrade : StockSharedModel
    {
        public DateTime PurchaseDate { get; set; }
        public decimal PurchasePoint { get; set; }
        public DateTime? SaleDate { get; set; }
        public decimal? SalePoint { get; set; }

    }
}
