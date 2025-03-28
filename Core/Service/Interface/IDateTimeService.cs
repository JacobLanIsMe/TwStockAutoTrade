using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service.Interface
{
    public interface IDateTimeService
    {
        DateTime GetTaiwanTime();
        DateTime ConvertTaiwaneseCalendarToGregorianCalendar(string taiwanDate);
    }
}
