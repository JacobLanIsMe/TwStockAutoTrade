using Core2.Model;
using Microsoft.Extensions.Logging;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Data.SqlClient;

namespace Core2.Repository
{
    public class CandidateRepository
    {
        private readonly ILogger<CandidateRepository> _logger;
        private readonly string _dbConnectionString;
        public CandidateRepository(ILogger<CandidateRepository> logger, string dbConnectionString)
        {
            _logger = logger;
            _dbConnectionString = dbConnectionString;
        }
        public async Task UpsertStockTech(List<StockTech> stockList)
        {
            _logger.LogInformation("Upsert stock tech data started.");
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
                parameters.Add("@StockList", table);
                await sqlConnection.ExecuteAsync("dbo.UpsertStockTech", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 600);
            }
            _logger.LogInformation("Upsert stock tech data finished.");
        }
    }
}
