using Core.Service.Interface;
using System;
using System.Globalization;

namespace Core.Service
{
    public class DateTimeService : IDateTimeService
    {
        public DateTime GetTaiwanTime()
        {
            TimeZoneInfo taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            DateTime taiwanTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
            return taiwanTime;
        }
        public DateTime ConvertTaiwaneseCalendarToGregorianCalendar(string taiwanDate)
        {
            if (int.TryParse(taiwanDate, out int taiwanDateInt))
            {
                if (DateTime.TryParseExact((taiwanDateInt + 19110000).ToString(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime gregorianCalendar))
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
        public DateTime ConvertTimestampToDateTime(int timestamp)
        {
            // Convert Unix timestamp to DateTime
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(timestamp).ToLocalTime();
        }
    }
}
