using Core.Model;
using Core.Repository.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
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

        public TradeRepository(IConfiguration config) 
        {
            _dbConnectionString = config.GetConnectionString("DefaultConnection");
        }
        public async Task<List<Trade>> GetStockHolding()
        {
            string sqlCommand = "SELECT * FROM [dbo].[Trade] WHERE [SaleDate] IS NULL";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                var result = await sqlConnection.QueryAsync<Trade>(sqlCommand);
                return result.ToList();
            }
        }
        public async Task UpdateLast9TechData(List<Trade> tradeList)
        {
            string sqlCommand = @"UPDATE [dbo].[Trade] SET [Last9TechData] = @Last9TechData WHERE [Id] = @Id";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                await sqlConnection.ExecuteAsync(sqlCommand, tradeList);
            }   
        }
    }
}
