using Core.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repository.Interface
{
    public interface ICandidateRepository
    {
        Task Insert(List<Candidate> candidateList);
        Task<List<Candidate>> GetActiveCandidate();
        Task UpdateIsDeleteById(List<Guid> IdList);
    }
}
