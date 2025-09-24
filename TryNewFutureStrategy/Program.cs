using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TryNewFutureStrategy
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = "C:\\Users\\Administrator\\Downloads\\台指1分K_2006-2020.csv";
            List<FutureCollection> futures = ReadFuturesFromCsv(filePath);

            var startTime = TimeSpan.Parse("08:46:00");
            var cutoffTime = TimeSpan.Parse("09:30:00");
            var lastEntryTime = TimeSpan.Parse("12:45:00");
            var endTime = TimeSpan.Parse("13:45:00");

            List<FutureCollection> selectedFutures = futures
                .Where(fc => fc.FutureList.All(f => f.TotalVolume != 0))
                .Select(fc => new FutureCollection
                {
                    Date = fc.Date,
                    FutureList = fc.FutureList
                        .Where(f => f.Time >= startTime && f.Time <= endTime)
                        .OrderBy(f => f.Time)
                        .ToList()
                })
                .Where(fc => fc.FutureList.Count > 0) // Ensure FutureList is not empty after filtering
                .OrderBy(fc => fc.Date)
                .ToList();
            List<TradeHistory> tradeList = new List<TradeHistory>();
            for (int i = 1; i < selectedFutures.Count; i++)
            {
                FutureCollection today = selectedFutures[i];
                FutureCollection yesterday = selectedFutures[i - 1];
                int yesterdayVolume = yesterday.FutureList.Sum(x => x.TotalVolume);
                var cutoff = today.FutureList.Where(x => x.Time >= startTime && x.Time <= cutoffTime);
                int todayHigh = cutoff.Max(x => x.High);
                int todayLow = cutoff.Min(x => x.Low);
                int todayVolume = cutoff.Sum(x => x.TotalVolume);
                if (todayHigh - todayLow <= 100 || todayVolume <= yesterdayVolume * 0.3) continue;
                int stopLossPoint = (int)(todayHigh - today.FutureList.First().Open * 0.004);
                List<Future> todayTech = today.FutureList.Where(x => x.Time > cutoffTime).ToList();
                TradeHistory trade = new TradeHistory();
                foreach (var j in todayTech)
                {
                    if (trade.EntryTime == TimeSpan.Zero)
                    {
                        if (j.High > todayHigh && j.Time <= lastEntryTime)
                        {
                            if (j.Low < stopLossPoint) break;
                            trade.Date = j.Date;
                            trade.EntryTime = j.Time;
                            trade.EntryPoint = todayHigh;
                            trade.Operation = "Long";
                        }
                    }
                    else
                    {
                        if (j.Low < stopLossPoint)
                        {
                            trade.ExitTime = j.Time;
                            trade.ExitPoint = stopLossPoint;
                        }
                    }
                }
                if (trade.EntryTime == TimeSpan.Zero) continue;
                if (trade.ExitTime == TimeSpan.Zero)
                {
                    var last = today.FutureList.Last();
                    trade.ExitTime = last.Time;
                    trade.ExitPoint = last.Close;
                }
                trade.ProfitLossPoint = trade.ExitPoint - trade.EntryPoint;
                trade.Result = trade.ProfitLossPoint > 0 ? "Win" : "Lose";
                tradeList.Add(trade);
            }
            int winCount = tradeList.Count(x => x.Result == "Win");
            int loseCount = tradeList.Count(x => x.Result == "Lose");
            int totalProfitLoss = tradeList.Sum(x => x.ProfitLossPoint);
            Console.WriteLine($"Total Trades: {tradeList.Count}, Wins: {winCount}, Losses: {loseCount}, Total P/L Points: {totalProfitLoss}");
            #region 寫入交易明細
            string outputFilePath = "C:\\Users\\Administrator\\Downloads\\result1.csv";

            using (var writer = new StreamWriter(outputFilePath))
            {
                // Write header
                writer.WriteLine("Date,EntryTime,EntryPoint,Operation,ExitTime,ExitPoint,Result,ProfitLossPoint");

                // Write trade details
                foreach (var trade in tradeList)
                {
                    writer.WriteLine($"{trade.Date:yyyy-MM-dd},{trade.EntryTime},{trade.EntryPoint},{trade.Operation},{trade.ExitTime},{trade.ExitPoint},{trade.Result},{trade.ProfitLossPoint}");
                    Console.WriteLine($"{trade.Date:yyyy-MM-dd},{trade.EntryTime},{trade.EntryPoint},{trade.Operation},{trade.ExitTime},{trade.ExitPoint},{trade.Result},{trade.ProfitLossPoint}");
                }
            }
            #endregion
        }

        static List<FutureCollection> ReadFuturesFromCsv(string filePath)
        {
            var futures = new List<Future>();

            using (var reader = new StreamReader(filePath))
            {
                string headerLine = reader.ReadLine(); // Skip header

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] values = line.Split(',');

                    try
                    {
                        var future = new Future
                        {
                            Date = ParseDate(values[0]),
                            Time = TimeSpan.ParseExact(values[1], @"hh\:mm\:ss", CultureInfo.InvariantCulture),
                            Open = (int)double.Parse(values[2]),
                            High = (int)double.Parse(values[3]),
                            Low = (int)double.Parse(values[4]),
                            Close = (int)double.Parse(values[5]),
                            TotalVolume = int.Parse(values[6])
                        };

                        futures.Add(future);
                    }
                    catch (FormatException ex)
                    {
                        Console.WriteLine($"Error parsing line: {line}. Exception: {ex.Message}");
                    }
                }
            }

            // Group futures by Date and create FutureCollection objects
            var futureCollections = futures
                .GroupBy(f => f.Date)
                .Select(group => new FutureCollection
                {
                    Date = group.Key,
                    FutureList = group.ToList()
                })
                .ToList();

            return futureCollections;
        }

        static DateTime ParseDate(string dateString)
        {
            string[] formats = { "yyyy/M/d", "yyyy/MM/dd" };
            if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate;
            }
            else
            {
                throw new FormatException($"Invalid date format: {dateString}");
            }
        }
    }
}
