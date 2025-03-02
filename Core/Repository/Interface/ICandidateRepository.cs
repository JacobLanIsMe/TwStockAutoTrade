using Core.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repository.Interface
{
    public interface ICandidateRepository
    {
        Task<List<Candidate>> GetActiveCandidate();
        Task Update(List<Guid> candidateToDeleteList, List<Candidate> candidateToUpdateList, List<Candidate> candidateToInsertList);
    }
}
