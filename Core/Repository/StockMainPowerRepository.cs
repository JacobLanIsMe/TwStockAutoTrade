using Core.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using Serilog;
using Core.Model;

namespace Core.Repository
{
    public class StockMainPowerRepository : IStockMainPowerRepository
    {
        private readonly string _dbConnectionString;
        private readonly ILogger _logger;

        public StockMainPowerRepository(IConfiguration config, ILogger logger)
        {
            _dbConnectionString = config.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public async Task Insert(List<StockMainPower> stockMainPowerList)
        {
            _logger.Information("Insert StockMainPower started.");
            string sqlCommand = @"INSERT INTO [dbo].[StockMainPower] (StockCode, CompanyName, MainPowerData, SelectedDate, TodayTechData) 
                                    VALUES (@StockCode, @CompanyName, @MainPowerData, @SelectedDate, @TodayTechData)";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                await sqlConnection.ExecuteAsync(sqlCommand, stockMainPowerList);
            }
            _logger.Information("Insert StockMainPower completed.");
        }

        public async Task<List<StockMainPower>> GetRecordsWithNullTomorrowTechData()
        {
            _logger.Information("Fetching records with null TomorrowTechData started.");
            string sqlCommand = @"SELECT * FROM [dbo].[StockMainPower] WHERE TomorrowTechData IS NULL";
            using (SqlConnection sqlConnection = new SqlConnection(_dbConnectionString))
            {
                var result = await sqlConnection.QueryAsync<StockMainPower>(sqlCommand);
                _logger.Information("Fetching records with null TomorrowTechData completed.");
                return result.ToList();
            }
        }
    }
}
