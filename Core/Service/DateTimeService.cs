using Core.Service.Interface;
using System;
using System.Globalization;

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
        public DateTime ConvertTaiwaneseCalendarToGregorianCalendar(string taiwanDate)
        {
            if (int.TryParse(taiwanDate, out int taiwanDateInt))
            {
                if (DateTime.TryParseExact((taiwanDateInt + 19110000).ToString(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime gregorianCalendar))
                {
                    return gregorianCalendar;
                }
                else
                {
                    return default;
                }
            }
            else
            {
                return default;
            }
        }
    }
}
