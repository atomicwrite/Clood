using Clood.Endpoints;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Serilog;
 
namespace Clood;

public class Program
{
    private static async Task Main(string[] args)
    {
        // Configure and initialize Serilog
        LogConfig.ConfigureLogging();
        var tempConfig = new ConfigurationBuilder()
            .AddCommandLine(args)
            .AddEnvironmentVariables()
            
            .Build();
        var isTestEnvironment = tempConfig["test"] == "true";
        var gitRoot = tempConfig["git-root"];
        var serverUrls = tempConfig["server-urls"];
        if (isTestEnvironment)
        {
            CloodServer.Start(serverUrls?? throw new InvalidOperationException("Server urls was null"),gitRoot ?? throw new InvalidOperationException("Git root was null"));
        }
        try
        {
            Log.Information("Starting Clood application");
            
            await Parser.Default.ParseArguments<CliOptions>(args)
                .WithParsedAsync(async (opts) => await Clood.RunWithOptions(opts));

            Log.Information("Clood application completed successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An unhandled exception occurred");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}