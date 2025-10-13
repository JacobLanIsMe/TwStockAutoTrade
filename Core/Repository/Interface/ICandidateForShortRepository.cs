using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repository.Interface
{
    public interface ICandidateForShortRepository
    {
        Task DeleteActiveCandidate();
        Task Insert(List<StockCandidate> candidateList);
        Task<List<StockCandidate>> GetActiveCandidate();
    }
}
