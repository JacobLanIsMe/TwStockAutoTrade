using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class TwotcExRrightsExDividend
    {
        public string ExRrightsExDividendDate { get; set; }
        public string SecuritiesCompanyCode { get; set; }
        public DateTime ExRrightsExDividendDateTime
        {
            get
            {
                if (int.TryParse(ExRrightsExDividendDate, out int exRrightsExDividendDate))
                {
                    if (DateTime.TryParseExact((exRrightsExDividendDate + 19110000).ToString(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime exRrightsExDividendDateTime))
                    {
                        return exRrightsExDividendDateTime;
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
}
