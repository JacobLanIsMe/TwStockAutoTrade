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
            var cutoffTime = TimeSpan.Parse("09:15:00");
            var lastEntryTime = TimeSpan.Parse("12:45:00");
            var endTime = TimeSpan.Parse("13:45:00");

            List<FutureCollection> selectedFutures = futures
                //.Where(fc => fc.FutureList.All(f => f.TotalVolume != 0))
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
            for (int i = 0; i < selectedFutures.Count; i++)
            {
                FutureCollection today = selectedFutures[i];
                int todayOpen = today.FutureList.First().Open;
                //FutureCollection yesterday = selectedFutures[i - 1];
                //int yesterdayVolume = yesterday.FutureList.Sum(x => x.TotalVolume);
                //int yesterdayClose = yesterday.FutureList.Last().Close;
                var cutoff = today.FutureList.Where(x => x.Time >= startTime && x.Time <= cutoffTime);
                int todayHigh = cutoff.Max(x => x.High);
                int todayLow = cutoff.Min(x => x.Low);
                //int todayVolume = cutoff.Sum(x => x.TotalVolume);
                int stopLossPoint = (int)(todayOpen * 0.004);
                if (todayHigh - todayLow <= stopLossPoint) continue;
                Dictionary<int, List<Future>> todayTech = today.FutureList.Where(x => x.Time > cutoffTime).GroupBy(ts => (int)(ts.Time.TotalMinutes / 15)).ToDictionary(g => g.Key, g => g.ToList());
                TradeHistory tradeHistory = new TradeHistory();
                foreach (var j in todayTech)
                {
                    if (tradeHistory.EntryTime == TimeSpan.Zero)
                    {
                        foreach (var k in j.Value)
                        {
                            if (tradeHistory.EntryTime == TimeSpan.Zero)
                            {
                                if (k.Time > lastEntryTime) break;

                                if (k.Low < todayLow)
                                {
                                    tradeHistory.Date = k.Date;
                                    tradeHistory.EntryTime = k.Time;
                                    tradeHistory.EntryPoint = todayLow;
                                    tradeHistory.Operation = "Short";
                                }
                                else if (k.High > todayHigh)
                                {
                                    tradeHistory.Date = k.Date;
                                    tradeHistory.EntryTime = k.Time;
                                    tradeHistory.EntryPoint = todayHigh;
                                    tradeHistory.Operation = "Long";
                                }
                            }
                            else
                            {
                                if (tradeHistory.Operation == "Short" && k.High > tradeHistory.EntryPoint + stopLossPoint)
                                {
                                    tradeHistory.ExitTime = k.Time;
                                    tradeHistory.ExitPoint = tradeHistory.EntryPoint + stopLossPoint;
                                }
                                else if (tradeHistory.Operation == "Long" && k.Low < tradeHistory.EntryPoint - stopLossPoint)
                                {
                                    tradeHistory.ExitTime = k.Time;
                                    tradeHistory.ExitPoint = tradeHistory.EntryPoint - stopLossPoint;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (tradeHistory.Operation == "Short" && j.Value.Any(x => x.High > tradeHistory.EntryPoint + stopLossPoint))
                        {
                            var exit = j.Value.First(x => x.High > tradeHistory.EntryPoint + stopLossPoint);
                            tradeHistory.ExitTime = exit.Time;
                            tradeHistory.ExitPoint = tradeHistory.EntryPoint + stopLossPoint;
                        }
                        else if (tradeHistory.Operation == "Long" && j.Value.Any(x => x.Low < tradeHistory.EntryPoint - stopLossPoint))
                        {
                            var exit = j.Value.First(x => x.Low < tradeHistory.EntryPoint - stopLossPoint);
                            tradeHistory.ExitTime = exit.Time;
                            tradeHistory.ExitPoint = tradeHistory.EntryPoint - stopLossPoint;
                        }
                    }
                    if (tradeHistory.ExitTime != TimeSpan.Zero) break;
                }
                if (tradeHistory.EntryTime == TimeSpan.Zero) continue;
                if (tradeHistory.ExitTime == TimeSpan.Zero)
                {
                    var last = today.FutureList.Last();
                    tradeHistory.ExitTime = last.Time;
                    tradeHistory.ExitPoint = last.Close;
                }
                tradeHistory.ProfitLossPoint = tradeHistory.Operation == "Short" ? tradeHistory.EntryPoint - tradeHistory.ExitPoint : tradeHistory.ExitPoint - tradeHistory.EntryPoint;
                tradeHistory.Result = tradeHistory.ProfitLossPoint > 0 ? "Win" : "Lose";
                tradeList.Add(tradeHistory);
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
