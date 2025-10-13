using Core.Service.Interface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class TwseSuspendedShortSellingStockInfo
    {
        public string Code { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}
