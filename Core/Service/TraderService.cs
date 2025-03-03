using Core.Model;
using Core.Repository.Interface;
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
        private readonly TradeConfig tradeConfig;
        private readonly ITradeRepository _tradeRepository;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IDateTimeService _dateTimeService;
        public TraderService(IConfiguration config, ITradeRepository tradeRepository, ICandidateRepository candidateRepository, IDateTimeService dateTimeService)
        {
            tradeConfig = config.GetSection("TradeConfig").Get<TradeConfig>();
            _tradeRepository = tradeRepository;
            _candidateRepository = candidateRepository;
            _dateTimeService = dateTimeService;
        }
        public async Task Trade()
        {
            List<Trade> stockHoldingList = await _tradeRepository.GetStockHolding();
            List<Candidate> candidateList = await _candidateRepository.GetActiveCandidate();
            
        }
    }
}
