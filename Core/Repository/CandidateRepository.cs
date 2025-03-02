using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repository
{
    public class CandidateRepository : ICandidateRepository
    {
        private readonly string _dbConnectionString;
        private readonly IDateTimeService _dateTimeService;
        public CandidateRepository(IConfiguration config, IDateTimeService dateTimeService)
        {
            _dbConnectionString = config.GetConnectionString("DefaultConnection");
            _dateTimeService = dateTimeService;
        }
        public async Task Insert(List<Candidate> candidateList)
        {
            if (!candidateList.Any()) return;
            string sqlCommand = @"INSERT INTO [dbo].[Candidate] 
                                ([Market], [StockCode], [CompanyName], [GapUpHigh], [GapUpLow], [StopLossPoint], [SelectedDate])
                                VALUES
                                (@Market, @StockCode, @CompanyName, @GapUpHigh, @GapUpLow, @StopLossPoint, @SelectedDate)";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                await sqlConnection.ExecuteAsync(sqlCommand, candidateList);
            }
        }
        public async Task<List<Candidate>> GetActiveCandidate()
        {
            string sqlCommand = $@"SELECT * FROM [dbo].[Candidate] WHERE IsDeleted = 0";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                var result = await sqlConnection.QueryAsync<Candidate>(sqlCommand);
                return result.ToList();
            }
        }
        public async Task UpdateIsDeleteById(List<Guid> IdList)
        {
            if (!IdList.Any()) return;
            DateTime deletedDate = _dateTimeService.GetTaiwanTime();
            string sqlCommand = $@"UPDATE [dbo].[Candidate] SET IsDeleted = 1 AND DeletedDate = @DeletedDate WHERE Id IN @IdList";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                await sqlConnection.ExecuteAsync(sqlCommand, new { DeletedDate = deletedDate, IdList = IdList });
            }
        }
    }
}
