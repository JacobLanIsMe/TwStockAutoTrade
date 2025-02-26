using Core.Service.Interface;
using System;

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
