using Core.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repository.Interface
{
    public interface ICandidateRepository
    {
        Task<List<StockCandidate>> GetActiveCandidate();
        Task<List<StockCandidate>> GetActiveCrazyCandidate();
        Task UpdateCandidate(List<Guid> candidateToDeleteList, List<StockCandidate> candidateToUpdateList, List<StockCandidate> candidateToInsertList);
        Task UpdateCrazyCandidate(List<Guid> candidateToDeleteList, List<StockCandidate> candidateToUpdateList, List<StockCandidate> candidateToInsertList);
        Task UpdateHoldingStock(List<StockCandidate> candidateToUpdateList);
        Task UpdateExRrightsExDividendDate(List<StockCandidate> candidateToUpdateList);
        Task UpsertStockTech(List<StockTech> stockList);
        Task<List<StockTech>> GetStockTech();
    }
}
