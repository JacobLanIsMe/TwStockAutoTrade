using Core2.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core2.Service
{
    public class StrategyService
    {
        private readonly MongoDbService _mongoDbService;
        public StrategyService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }
        public async Task ExecuteStrategy()
        {
            List<StockTech> stockTechList = await _mongoDbService.GetAllStockTechAsync();
            List<StockTech> candidate = new List<StockTech>();
            foreach (var i in stockTechList)
            {
                StockTechData? todayTechData = i.TechDataList.FirstOrDefault();
                if (todayTechData == null) continue;
                StockTechData? jumpTechData = null;
                for (int j = 1; j < i.TechDataList.Count - 1; j++)
                {
                    if (i.TechDataList[j].Low > i.TechDataList[j + 1].High)
                    {
                        jumpTechData = i.TechDataList[j];
                        break;
                    }
                }
                if (jumpTechData == null) continue;
                // jumpTechData is checked for null above, use null-forgiving operator to access its members
                decimal periodMaxHigh = i.TechDataList.Skip(1).Where(x => x.Date >= jumpTechData!.Date).Max(x => x.High);
                
                if (todayTechData.Close > periodMaxHigh)
                {
                    candidate.Add(i);
                    Console.WriteLine("Stock {StockCode} ({CompanyName}) is a candidate. Today's Close: {Close}, Jump's Date: {Jump's Date}, Period Max High: {MaxHigh}", i.StockCode, i.CompanyName, todayTechData.Close, jumpTechData.Date, periodMaxHigh);
                }
            }
        }
    }
}
