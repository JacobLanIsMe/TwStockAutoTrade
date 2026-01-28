using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Core2.Model
{
    public class TwseCompanyInfo
    {
        [JsonPropertyName("公司代號")]
        public string StockCode { get; set; }
        [JsonPropertyName("已發行普通股數或TDR原股發行股數")]
        public string _IssuedShare { get; set; }
        public long IssuedShare
        {
            get => long.TryParse(_IssuedShare, out long result) ? result : 0;
        }
    }
}
