using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class TwseSuspendedShortSellingStockInfo
    {
        [JsonProperty("Code")]
        public string StockCode { get; set; }
        [JsonProperty("StartDate")]
        public string _StartDate { get; set; }
        [JsonProperty("EndDate")]
        public string _EndDate { get; set; }
    }
}
