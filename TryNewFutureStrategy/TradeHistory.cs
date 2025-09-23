using System;

namespace TryNewFutureStrategy
{
    public class TradeHistory
    {
        public DateTime Date { get; set; }
        public TimeSpan EntryTime { get; set; }
        public int EntryPoint { get; set; }
        public string Operation { get; set; }
        public TimeSpan ExitTime { get; set; }
        public int ExitPoint { get; set; }
        public string Result { get; set; }
        public int ProfitLossPoint { get; set; }
    }
}