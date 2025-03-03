using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class Trade : StockSharedModel
    {
        public int PurchasedLot { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal PurchaseAmount { get; set; }
        public DateTime? SaleDate { get; set; }
        public decimal? SaleAmount { get; set; }

    }
}
