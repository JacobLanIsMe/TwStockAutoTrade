using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class StockMainPower
    {
        public string StockCode { get; set; }
        public string CompanyName { get; set; }
        public string MainPowerData { get; set; }
        public List<MainPower> MainPowerDataList { get; set; } = new List<MainPower>();
    }
    public class MainPower
    {
        public DateTime Date { get; set; }
        public int MainPowerVolume { get; set; }
    }
}
