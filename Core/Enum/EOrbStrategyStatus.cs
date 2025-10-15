using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Enum
{
    public enum EOrbStrategyStatus
    {
        /// <summary>
        /// 尚未進行判斷或判斷條件尚未滿足（例如，交易日還沒開始或 ORB 區間尚未結束）。
        /// </summary>
        Undetermined = 0,

        /// <summary>
        /// 符合 ORB 策略的進場或操作條件。
        /// </summary>
        Match = 1,

        /// <summary>
        /// 不符合 ORB 策略的進場或操作條件。
        /// </summary>
        NoMatch = 2
    }
}
