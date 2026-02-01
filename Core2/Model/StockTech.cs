using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Core2.Model
{
    public class StockTech
    {
        public string StockCode { get; set; }
        public string CompanyName { get; set; }
        public long IssuedShare { get; set; }
        public string TechData { get; set; }
        public List<StockTechData> TechDataList
        {
            get => string.IsNullOrEmpty(TechData) ? new List<StockTechData>() : JsonSerializer.Deserialize<List<StockTechData>>(TechData);
        }
    }
}
