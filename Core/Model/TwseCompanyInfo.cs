using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class TwseCompanyInfo
    {
        [JsonProperty("公司代號")]
        public string StockCode { get; set; }
        [JsonProperty("已發行普通股數或TDR原股發行股數")]
        public string _IssuedShares { get; set; }
        public long IssuedShares
        {
            get => long.TryParse(_IssuedShares, out long result) ? result : 0;
        }
    }
}
