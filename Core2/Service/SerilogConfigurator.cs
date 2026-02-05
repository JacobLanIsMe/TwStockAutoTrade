using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Core2.Service
{
    public static class SerilogConfigurator
    {
        public static IServiceCollection AddSerilogLogging(this IServiceCollection services, string logDirectory)
        {
            if (string.IsNullOrWhiteSpace(logDirectory))
                throw new ArgumentException("logDirectory must be provided", nameof(logDirectory));

            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "log-.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.File(
                    path: logPath,
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: false);
            });

            return services;
        }

        public static void Shutdown()
        {
            try
            {
                Log.CloseAndFlush();
            }
            catch
            {
                // ignore
            }
        }
    }
}
