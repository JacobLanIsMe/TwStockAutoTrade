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
            for (int i = 1; i < selectedFutures.Count; i++)
            {
                int yesterdayVolume = selectedFutures[i - 1].FutureList.Sum(x => x.TotalVolume);
                var cutoff = selectedFutures[i].FutureList.Where(x => x.Time >= startTime && x.Time <= cutoffTime);
                int todayHigh = cutoff.Max(x => x.High);
                int todayLow = cutoff.Min(x => x.Low);
                int todayVolume = cutoff.Sum(x => x.TotalVolume);
                if (todayHigh - todayLow <= 100 || todayVolume <= yesterdayVolume * 0.3) continue;
                List<Future> today = selectedFutures[i].FutureList;
                
            }
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
