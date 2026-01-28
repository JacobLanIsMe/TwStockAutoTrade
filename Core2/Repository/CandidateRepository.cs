using Core2.Model;
using Microsoft.Extensions.Logging;
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
        public CandidateRepository(ILogger<CandidateRepository> logger, string dbConnectionString)
        {
            _logger = logger;
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
                parameters.Add("@StockList", table.AsTableValuedParameter("dbo.StockTechType"));
                await sqlConnection.ExecuteAsync("dbo.UpsertStockTech", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 600);
            }
            _logger.Information("Upsert stock tech data finished.");
        }
    }
}
