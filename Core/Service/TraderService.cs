using Core.Model;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class TraderService : ITraderService
    {
        public TraderService(IConfiguration config)
        {
            TradeConfig tradeConfig = config.GetSection("TradeConfig").Get<TradeConfig>();
        }
        public async Task Trade()
        {

        }
    }
}
