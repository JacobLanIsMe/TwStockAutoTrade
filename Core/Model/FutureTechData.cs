using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Model
{
    public class FutureTechData
    {
        public string Date { get; set; }
        public string Contract { get; set; }
        [JsonProperty("ContractMonth(Week)")]
        public string ContractMonth { get; set; }
        public string Last { get; set; }
        public string TradingSession { get; set; }
        public DateTime DateTime
        {
            get
            {
                return DateTime.ParseExact(Date, "yyyyMMdd", null);
            }
        }
    }
}
