using Core2.Model;
using MongoDB.Bson;
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
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<StockTech> _collection;

        // timezone helpers removed; DateTime values are stored as provided

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
            _database = client.GetDatabase(dbName);
            _collection = _database.GetCollection<StockTech>("StockExchangeReport");
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

        // Retrieve all StockTech documents from the 'StockExchangeReport' collection
        // Only read the fields that exist on the StockTech class to avoid deserialization errors
        public async Task<List<StockTech>> GetAllStockTechAsync()
        {
            var bsonCol = _database.GetCollection<BsonDocument>("StockExchangeReport");
            var projection = Builders<BsonDocument>.Projection
                .Include("StockCode")
                .Include("CompanyName")
                .Include("IssuedShare")
                .Include("TechData")
                .Exclude("_id");

            var docs = await bsonCol.Find(Builders<BsonDocument>.Filter.Empty)
                                     .Project(projection)
                                     .ToListAsync().ConfigureAwait(false);

            var list = new List<StockTech>(docs.Count);
            foreach (var d in docs)
            {
                long issuedShare = 0;
                if (d.Contains("IssuedShare"))
                {
                    var v = d["IssuedShare"];
                    if (v.IsInt64) issuedShare = v.AsInt64;
                    else if (v.IsInt32) issuedShare = v.AsInt32;
                    else if (v.IsDouble) issuedShare = (long)v.AsDouble;
                    else long.TryParse(v.ToString(), out issuedShare);
                }

                var st = new StockTech
                {
                    StockCode = d.Contains("StockCode") ? d["StockCode"].AsString : null,
                    CompanyName = d.Contains("CompanyName") ? d["CompanyName"].AsString : null,
                    IssuedShare = issuedShare,
                    TechData = d.Contains("TechData") ? d["TechData"].AsString : null
                };
                list.Add(st);
            }

            return list;
        }

        // Sync candidate list to 'Candidate' collection according to rules:
        // - Insert new candidate docs for items in candidateList (TechDataList first 5, IsCandidate=true, SelectedDate = first TechData Date)
        // - Do not insert if same StockCode+SelectedDate and IsCandidate==true already exists
        // - For existing Candidate docs with IsCandidate==true that are not present in new list, set IsCandidate=false
        public async Task SyncCandidates(List<StockCandidate> candidateList)
        {
            if (candidateList == null) return;

            // Build new docs keyed by StockCode + SelectedDate
            var newDocs = new List<(string Key, BsonDocument Doc)>();
            foreach (var c in candidateList)
            {
                if (c.TechDataList == null || c.TechDataList.Count == 0) continue;
                var top5 = c.TechDataList.Take(5).Select(td => new BsonDocument
                {
                    // store Date as UTC
                    { "Date", new BsonDateTime(td.Date.ToUniversalTime()) },
                    { "Close", (double)td.Close },
                    { "Open", (double)td.Open },
                    { "High", (double)td.High },
                    { "Low", (double)td.Low },
                    { "Volume", td.Volume }
                }).ToList();

                var selectedDate = c.TechDataList.First().Date;
                // use UTC representation for keys to avoid mismatch between stored BSON Date kinds
                var key = c.StockCode + "|" + selectedDate.ToUniversalTime().ToString("o");
                var doc = new BsonDocument
                {
                    { "StockCode", c.StockCode },
                    { "CompanyName", c.CompanyName },
                    { "Market", c.Market.ToString() },
                    { "IsCandidate", true },
                    // store SelectedDate as BSON Date in UTC
                    { "SelectedDate", new BsonDateTime(selectedDate.ToUniversalTime()) },
                    
                    { "TechDataList", new BsonArray(top5) }
                };
                newDocs.Add((key, doc));
            }

            var candidateCol = _database.GetCollection<BsonDocument>("Candidate");

            // Get existing candidate docs with IsCandidate == true
            var filterActive = Builders<BsonDocument>.Filter.Eq("IsCandidate", true);
            var existing = await candidateCol.Find(filterActive).ToListAsync();

            var existingKeys = new HashSet<string>(existing.Select(d =>
            {
                var stock = d.Contains("StockCode") ? d["StockCode"].AsString : string.Empty;
                string sdStr;
                if (d.Contains("SelectedDate") && d["SelectedDate"].IsBsonDateTime)
                {
                    sdStr = d["SelectedDate"].AsBsonDateTime.ToUniversalTime().ToString("o");
                }
                else
                {
                    sdStr = DateTime.MinValue.ToString("o");
                }
                return stock + "|" + sdStr;
            }));

            var newKeys = new HashSet<string>(newDocs.Select(n => n.Key));

            // Inserts: newDocs that are not in existingKeys
            var toInsert = newDocs.Where(n => !existingKeys.Contains(n.Key)).Select(n => n.Doc).ToList();
            if (toInsert.Count > 0)
            {
                await candidateCol.InsertManyAsync(toInsert);
            }

            // Deactivate: existing docs that are not present in newKeys -> set IsCandidate = false
            var toDeactivate = existing.Where(d =>
            {
                var stock = d.Contains("StockCode") ? d["StockCode"].AsString : string.Empty;
                DateTime sd;
                if (d.Contains("SelectedDate") && d["SelectedDate"].IsBsonDateTime)
                {
                    sd = d["SelectedDate"].AsBsonDateTime.ToUniversalTime();
                }
                else
                {
                    sd = DateTime.MinValue;
                }
                var key = stock + "|" + sd.ToString("o");
                return !newKeys.Contains(key);
            }).ToList();

            if (toDeactivate.Count > 0)
            {
                var bulk = new List<WriteModel<BsonDocument>>();
                foreach (var d in toDeactivate)
                {
                    var stock = d.Contains("StockCode") ? d["StockCode"].AsString : string.Empty;
                    DateTime sd;
                    if (d.Contains("SelectedDate") && d["SelectedDate"].IsBsonDateTime)
                    {
                        sd = d["SelectedDate"].AsBsonDateTime.ToUniversalTime();
                    }
                    else
                    {
                        sd = DateTime.MinValue;
                    }
                    var f = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("StockCode", stock),
                        Builders<BsonDocument>.Filter.Eq("SelectedDate", sd),
                        Builders<BsonDocument>.Filter.Eq("IsCandidate", true)
                    );
                    var u = Builders<BsonDocument>.Update.Set("IsCandidate", false);
                    bulk.Add(new UpdateOneModel<BsonDocument>(f, u));
                }
                if (bulk.Count > 0)
                {
                    await candidateCol.BulkWriteAsync(bulk, new BulkWriteOptions { IsOrdered = false }).ConfigureAwait(false);
                }
            }
        }
    }
}
