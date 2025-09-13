using Core.Model;
using Core.Repository.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repository
{
    public class CandidateForShortRepository : ICandidateForShortRepository
    {
        private readonly string _dbConnectionString;
        private readonly ILogger _logger;
        public CandidateForShortRepository(IConfiguration config, ILogger logger)
        {
            _dbConnectionString = _dbConnectionString = config.GetConnectionString("DefaultConnection");
            _logger = logger;
        }
        public async Task DeleteActiveCandidate()
        {
            _logger.Information($"Delete active candidate started.");
            string sqlCommand = @"UPDATE [dbo].[CandidateForShort] SET IsDeleted = 1 WHERE IsDeleted = 0";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                await sqlConnection.ExecuteAsync(sqlCommand);
            }
            _logger.Information($"Delete active candidate completed.");
        }
        public async Task Insert(List<StockCandidate> candidateList)
        {
            _logger.Information($"Insert candidate started.");
            string sqlCommand = @"INSERT INTO [dbo].[CandidateForShort] (StockCode, CompanyName, SelectedDate, LimitUpPrice, PriceBeforeLimitUp) VALUES (@StockCode, @CompanyName, @SelectedDate, @LimitUpPrice, @PriceBeforeLimitUp)";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                await sqlConnection.ExecuteAsync(sqlCommand, candidateList);
            }
            _logger.Information($"Insert candidate completed.");
        }
    }
}
