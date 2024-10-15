using Serilog;

namespace Clood;

public static class LogConfig
{
    public static void ConfigureLogging()
    {
        string logPath = DetermineLogPath();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information($"Logging to: {logPath}");
    }

    private static string DetermineLogPath()
    {
        if (!OperatingSystem.IsLinux())
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "clood-.log");
        const string linuxLogPath = "/var/log/clood-.log";
        if (CanWriteToPath(linuxLogPath))
        {
            return linuxLogPath;
        }
        Log.Warning("Cannot write to /var/log. Falling back to local directory.");

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "clood-.log");
    }

    private static bool CanWriteToPath(string path)
    {
        try
        {
            using (File.Create(path, 1, FileOptions.DeleteOnClose)) { }
            return true;
        }
        catch
        {
            return false;
        }
    }
}