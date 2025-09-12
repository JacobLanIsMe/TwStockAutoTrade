using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class TwotcSuspendedShortSellingStockInfo
    {
        public string SecuritiesCompanyCode { get; set; }
        public string FirstDayToSuspendSellThenBuy { get; set; }
        public string DayOfReinstatingSellThenBuy { get; set; }
    }
}
