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
        public int SqlId { get; set; }
        public Guid Id { get; set; }
        public string StockCode { get; set; }
        public string CompanyName { get; set; }
        public string MainPowerData { get; set; }
        public DateTime SelectedDate { get; set; }
        public string TodayTechData { get; set; }
        public string TomorrowTechData { get; set; }
    }
}
