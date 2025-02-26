using Core.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class DateTimeService : IDateTimeService
    {
        public DateTime GetTaiwanTime()
        {
            DateTime now = DateTime.UtcNow; 
            TimeZoneInfo taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            DateTime taiwanTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
            return taiwanTime;
        }
    }
}
