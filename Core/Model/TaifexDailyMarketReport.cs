using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Model
{
    public class TaifexDailyMarketReport
    {
        public string Date { get; set; }
        public string Contract { get; set; }
        [JsonProperty("ContractMonth(Week)")]
        public string ContractMonth { get; set; }
        public string Volume { get; set; }
        public string TradingSession { get; set; }
        public string SettlementPrice { get; set; }
    }
}
