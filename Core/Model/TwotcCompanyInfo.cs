using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class TwotcCompanyInfo
    {
        public string SecuritiesCompanyCode { get; set; }
        [JsonProperty("IssueShares")]
        public string _IssuedShare { get; set; }
        public long IssuedShare
        {
            get => long.TryParse(_IssuedShare, out long result) ? result : 0;
        }
    }
}
