using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
using Serilog;
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
        private readonly ILogger _logger;
        public CandidateRepository(IConfiguration config, IDateTimeService dateTimeService, ILogger logger)
        {
            _dbConnectionString = config.GetConnectionString("DefaultConnection");
            _dateTimeService = dateTimeService;
            _logger = logger;
        }
        public async Task<List<StockCandidate>> GetActiveCandidate()
        {
            _logger.Information("Get candidate started.");
            string sqlCommand = $@"SELECT * FROM [dbo].[Candidate] WHERE IsDeleted = 0";
            IEnumerable<StockCandidate> result;
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                result = await sqlConnection.QueryAsync<StockCandidate>(sqlCommand);
            }
            _logger.Information("Get candidate finished.");
            return result.ToList();
        }
        public async Task Update(List<Guid> candidateToDeleteList, List<StockCandidate> candidateToUpdateList, List<StockCandidate> candidateToInsertList)
        {
            _logger.Information("Update candidate started.");
            DateTime deletedDate = _dateTimeService.GetTaiwanTime();
            string deleteSqlCommand = @"UPDATE [dbo].[Candidate] SET [IsDeleted] = 1, [DeletedDate] = @DeletedDate WHERE [Id] IN @IdList";
            string updateSqlCommand = @"UPDATE [dbo].[Candidate] SET [Last9TechData] = @Last9TechData WHERE [Id] = @Id";
            string insertSqlCommand = @"INSERT INTO [dbo].[Candidate] 
                                ([Market], [StockCode], [CompanyName], [GapUpHigh], [GapUpLow], [EntryPoint], [StopLossPoint], [SelectedDate], [Last9TechData])
                                VALUES
                                (@Market, @StockCode, @CompanyName, @GapUpHigh, @GapUpLow, @EntryPoint, @StopLossPoint, @SelectedDate, @Last9TechData)";

            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                await sqlConnection.OpenAsync();
                using (var transaction = sqlConnection.BeginTransaction())
                {
                    if (candidateToDeleteList.Any())
                    {
                        await sqlConnection.ExecuteAsync(deleteSqlCommand, new { DeletedDate = deletedDate, IdList = candidateToDeleteList }, transaction: transaction);
                    }
                    if (candidateToUpdateList.Any())
                    {
                        await sqlConnection.ExecuteAsync(updateSqlCommand, candidateToUpdateList, transaction: transaction);
                    }
                    if (candidateToInsertList.Any())
                    {
                        await sqlConnection.ExecuteAsync(insertSqlCommand, candidateToInsertList, transaction: transaction);
                    }
                    transaction.Commit();
                }
            }
            _logger.Information("Update candidate finished.");
        }
    }
}
