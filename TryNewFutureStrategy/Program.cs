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
            List<Future> futures = ReadFuturesFromCsv(filePath);

            // Example: Print the first Future object
            if (futures.Count > 0)
            {
                Console.WriteLine($"Date: {futures[0].Date}, Time: {futures[0].Time}, Open: {futures[0].Open}, High: {futures[0].High}, Low: {futures[0].Low}, Close: {futures[0].Close}, TotalVolume: {futures[0].TotalVolume}");
            }
        }

        static List<Future> ReadFuturesFromCsv(string filePath)
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

            return futures;
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
