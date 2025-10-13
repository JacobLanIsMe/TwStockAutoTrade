using System;
using System.Collections.Generic;

namespace Core.Model
{
    public class MainInOutDetailResponse
    {
        public MainInOutData Data { get; set; }
        public int Status { get; set; }
    }

    public class MainInOutData
    {
        public List<MainDetail> MainInDetails { get; set; }
        public List<MainDetail> MainOutDetails { get; set; }
        public int Summary { get; set; }
        public string UpdateDate { get; set; }
    }

    public class MainDetail
    {
        public string SecuritiesCompName { get; set; }
        public string SecuritiesCompSymbol { get; set; }
        public double Count { get; set; }
        public double Price { get; set; }
        public double BuyCount { get; set; }
        public double SellCount { get; set; }
        public double TradeRatio { get; set; }
    }
}