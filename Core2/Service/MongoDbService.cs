using Core2.Model;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core2.Service
{
    // Performs efficient bulk upsert of StockTech documents into MongoDB.
    public class MongoDbService
    {
        private readonly IMongoCollection<StockTech> _collection;

        public MongoDbService()
        {
            // Try machine-level first, then process-level environment variable
            var connString = Environment.GetEnvironmentVariable("MongoDb", EnvironmentVariableTarget.Machine)
                             ?? Environment.GetEnvironmentVariable("MongoDb");
            if (string.IsNullOrWhiteSpace(connString))
                throw new InvalidOperationException("MongoDb connection string not found in environment variable 'MongoDb'.");

            var url = new MongoUrl(connString);
            var client = new MongoClient(url);
            var dbName = string.IsNullOrEmpty(url.DatabaseName) ? "MyFuture" : url.DatabaseName;
            var db = client.GetDatabase(dbName);
            _collection = db.GetCollection<StockTech>("StockExchangeReport");
        }

        // Upsert list of StockTech documents. Uses unordered bulk operations and batches for performance.
        public async Task UpsertStockTech(List<StockTech> stockTechList)
        {
            if (stockTechList == null || stockTechList.Count == 0) return;

            const int batchSize = 1000; // tune this value if needed
            var batches = stockTechList
                .Select((item, idx) => new { item, idx })
                .GroupBy(x => x.idx / batchSize, x => x.item)
                .Select(g => g.ToList());

            foreach (var batch in batches)
            {
                var models = new List<WriteModel<StockTech>>(batch.Count);
                foreach (var stock in batch)
                {
                    var filter = Builders<StockTech>.Filter.Eq(x => x.StockCode, stock.StockCode);
                    var update = Builders<StockTech>.Update
                        .Set(x => x.CompanyName, stock.CompanyName)
                        .Set(x => x.IssuedShare, stock.IssuedShare)
                        .Set(x => x.TechData, stock.TechData);

                    models.Add(new UpdateOneModel<StockTech>(filter, update) { IsUpsert = true });
                }

                if (models.Count > 0)
                {
                    var options = new BulkWriteOptions { IsOrdered = false };
                    await _collection.BulkWriteAsync(models, options).ConfigureAwait(false);
                }
            }
        }
    }
}
