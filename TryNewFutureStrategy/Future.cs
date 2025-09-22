using System;

namespace TryNewFutureStrategy
{
    public class Future
    {
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public int Open { get; set; }
        public int High { get; set; }
        public int Low { get; set; }
        public int Close { get; set; }
        public int TotalVolume { get; set; }
    }
}