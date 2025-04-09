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
        [JsonProperty("mem")]
        public Mem Mem { get; set; }
    }
    public class Mem
    {
        [JsonProperty("129")]
        public decimal SettlementPrice { get; set; }
    }
}
