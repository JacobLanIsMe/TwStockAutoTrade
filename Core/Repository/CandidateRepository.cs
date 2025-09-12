using Core.Model;
using Core.Repository.Interface;
using Core.Service.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
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
        public async Task<List<StockCandidate>> GetActiveCrazyCandidate()
        {
            _logger.Information("Get candidate started.");
            string sqlCommand = $@"SELECT * FROM [dbo].[CrazyCandidate] WHERE IsDeleted = 0";
            IEnumerable<StockCandidate> result;
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                result = await sqlConnection.QueryAsync<StockCandidate>(sqlCommand);
            }
            _logger.Information("Get candidate finished.");
            return result.ToList();
        }
        public async Task UpdateCandidate(List<Guid> candidateToDeleteList, List<StockCandidate> candidateToUpdateList, List<StockCandidate> candidateToInsertList)
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
        public async Task UpdateCrazyCandidate(List<Guid> candidateToDeleteList, List<StockCandidate> candidateToUpdateList, List<StockCandidate> candidateToInsertList)
        {
            _logger.Information("Update crazy candidate started.");
            DateTime deletedDate = _dateTimeService.GetTaiwanTime();
            string deleteSqlCommand = @"UPDATE [dbo].[CrazyCandidate] SET [IsDeleted] = 1, [DeletedDate] = @DeletedDate WHERE [Id] IN @IdList";
            string updateSqlCommand = @"UPDATE [dbo].[CrazyCandidate] SET [Last9TechData] = @Last9TechData WHERE [Id] = @Id";
            string insertSqlCommand = @"INSERT INTO [dbo].[CrazyCandidate] 
                                        ([Market], [StockCode], [CompanyName], [EntryPoint], [StopLossPoint], [SelectedDate], [Last9TechData])
                                        VALUES
                                        (@Market, @StockCode, @CompanyName, @EntryPoint, @StopLossPoint, @SelectedDate, @Last9TechData)";
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
            _logger.Information("Update crazy candidate finished.");
        }
        public async Task UpdateHoldingStock(List<StockCandidate> candidateToUpdateList)
        {
            if (!candidateToUpdateList.Any()) return;
            string updateSqlCommand = @"UPDATE [dbo].[Candidate] SET [PurchasedLot] = @PurchasedLot WHERE [Id] = @Id";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                await sqlConnection.ExecuteAsync(updateSqlCommand, candidateToUpdateList);
            }
        }
        public async Task UpdateExRrightsExDividendDate(List<StockCandidate> candidateToUpdateList)
        {
            if (!candidateToUpdateList.Any()) return;
            string updateSqlCommand = @"UPDATE [dbo].[Candidate] SET [ExRrightsExDividendDateTime] = @ExRrightsExDividendDateTime WHERE [Id] = @Id";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                await sqlConnection.ExecuteAsync(updateSqlCommand, candidateToUpdateList);
            }
        }
        
        public async Task UpsertStockTech(List<StockTech> stockList)
        {
            _logger.Information("Upsert stock tech data started.");
            var table = new DataTable();
            table.Columns.Add("StockCode", typeof(string));
            table.Columns.Add("CompanyName", typeof(string));
            table.Columns.Add("IssuedShare", typeof(long));
            table.Columns.Add("TechData", typeof(string));

            foreach (var stock in stockList)
            {
                table.Rows.Add(stock.StockCode, stock.CompanyName, stock.IssuedShare, stock.TechData);
            }
            using (var sqlConnection = new SqlConnection(_dbConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@StockList", table.AsTableValuedParameter("dbo.StockTechType"));
                await sqlConnection.ExecuteAsync("dbo.UpsertStockTech", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 600);
            }
            _logger.Information("Upsert stock tech data finished.");
        }
        public async Task<List<StockTech>> GetStockTech()
        {
            string sqlCommand = "SELECT [StockCode] ,[CompanyName] ,[TechData] FROM dbo.StockTech";
            IEnumerable<StockTech> result;
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                result = await sqlConnection.QueryAsync<StockTech>(sqlCommand);
            }
            return result.ToList();
        }
        public async Task UpsertStockMainPower(List<StockMainPower> stockList)
        {
            _logger.Information("Upsert stock main power started.");
            var table = new DataTable();
            table.Columns.Add("StockCode", typeof(string));
            table.Columns.Add("CompanyName", typeof(string));
            table.Columns.Add("MainPowerData", typeof(string));
            foreach (var stock in stockList)
            {
                table.Rows.Add(stock.StockCode, stock.CompanyName, stock.MainPowerData);
            }
            using (var sqlConnection = new SqlConnection(_dbConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@StockList", table.AsTableValuedParameter("dbo.StockMainPowerType"));
                await sqlConnection.ExecuteAsync("dbo.UpsertStockMainPower", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 600);
            }
            _logger.Information("Upsert stock main power finished.");
        }
        public async Task<List<StockMainPower>> GetStockMainPower()
        {
            string sqlCommand = "SELECT [StockCode] ,[CompanyName] ,[MainPowerData] FROM dbo.StockMainPower";
            IEnumerable<StockMainPower> result;
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                result = await sqlConnection.QueryAsync<StockMainPower>(sqlCommand);
            }
            return result.ToList();
        }
    }
}
