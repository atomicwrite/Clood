using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace Clood
{
    public static class Clog
    {
        private static ILogger? _logger;

        public static void Initialize()
        {
            if (_logger != null)
            {
                return; // Logger is already initialized
            }

            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clood.log");

            _logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Verbose)
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Verbose)
                .CreateLogger();

            _logger.Information("Logging initialized");
        }

        public static void Verbose(string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Verbose(messageTemplate, propertyValues);
        }

        public static void Debug(string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Debug(messageTemplate, propertyValues);
        }

        public static void Information(string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Information(messageTemplate, propertyValues);
        }

        public static void Warning(string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Warning(messageTemplate, propertyValues);
        }

        public static void Error(string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Error(messageTemplate, propertyValues);
        }

        public static void Fatal(string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Fatal(messageTemplate, propertyValues);
        }

        public static void Exception(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Error(exception, messageTemplate, propertyValues);
        }

        private static void EnsureInitialized()
        {
            if (_logger == null)
            {
                Initialize();
            }
        }
    }
}
