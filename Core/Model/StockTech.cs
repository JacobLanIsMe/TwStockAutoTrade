﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class StockTech
    {
        public string StockCode { get; set; }
        public string CompanyName { get; set; } 
        public long IssuedShare { get; set; }
        public string TechData { get; set; }
        public List<StockTechData> TechDataList
        {
            get => JsonConvert.DeserializeObject<List<StockTechData>>(TechData);
        }
    }
}
