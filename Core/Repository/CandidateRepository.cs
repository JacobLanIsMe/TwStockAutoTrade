using Core.Model;
using Core.Repository.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repository
{
    public class CandidateRepository : ICandidateRepository
    {
        private readonly string _dbConnectionString;
        public CandidateRepository(IConfiguration config)
        {
            _dbConnectionString = config.GetConnectionString("DefaultConnection");
        }
        public async Task Insert(List<Candidate> candidateList)
        {
            if (!candidateList.Any()) return;
            string sqlCommand = $@"INSERT INTO [dbo].[Candidate] 
                                ([Market], [StockCode], [CompanyName], [EntryPoint], [StopLossPoint], [SelectedDate])
                                VALUES
                                (@Market, @StockCode, @CompanyName, @EntryPoint, @StopLossPoint @SelectedDate)";
            using(SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                await sqlConnection.ExecuteAsync(sqlCommand, candidateList);
            }
        }
        public async Task<List<Candidate>> GetActiveCandidate()
        {
            using(SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                string sqlCommand = $@"SELECT * FROM [dbo].[Candidate] WHERE IsDeleted = 0";
                var result = await sqlConnection.QueryAsync<Candidate>(sqlCommand);
                return result.ToList();
            }
        }
    }
}
