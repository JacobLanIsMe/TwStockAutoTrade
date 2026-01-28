using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Core2.Model
{
    public class TwotcCompanyInfo
    {
        public string SecuritiesCompanyCode { get; set; }
        [JsonPropertyName("IssueShares")]
        public string _IssuedShare { get; set; }
        public long IssuedShare
        {
            get => long.TryParse(_IssuedShare, out long result) ? result : 0;
        }
    }
}
