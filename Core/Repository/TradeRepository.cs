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
    public class TradeRepository : ITradeRepository
    {
        private readonly string _dbConnectionString;
        private readonly ILogger _logger;

        public TradeRepository(IConfiguration config, ILogger logger) 
        {
            _dbConnectionString = config.GetConnectionString("DefaultConnection");
            _logger = logger;
        }
        public async Task<List<StockTrade>> GetStockHolding()
        {
            _logger.Information("Get stock holdings started.");
            string sqlCommand = "SELECT * FROM [dbo].[Trade] WHERE [SaleDate] IS NULL";
            IEnumerable<StockTrade> result;
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                result = await sqlConnection.QueryAsync<StockTrade>(sqlCommand);
            }
            _logger.Information("Get stock holdings finished.");
            return result.ToList();
        }
        public async Task UpdateLast9TechData(List<StockTrade> tradeList)
        {
            _logger.Information("Update Last9TechData of table Trader started.");
            string sqlCommand = @"UPDATE [dbo].[Trade] SET [Last9TechData] = @Last9TechData WHERE [Id] = @Id";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                await sqlConnection.ExecuteAsync(sqlCommand, tradeList);
            }   
            _logger.Information("Update Last9TechData of table Trader finished.");
        }
    }
}
