using Core.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repository.Interface
{
    public interface ICandidateRepository
    {
        Task<List<StockCandidate>> GetActiveCandidate();
        Task Update(List<Guid> candidateToDeleteList, List<StockCandidate> candidateToUpdateList, List<StockCandidate> candidateToInsertList);
        Task UpdateCrazyCandidate(List<StockCandidate> candidateToInsertList);
    }
}
