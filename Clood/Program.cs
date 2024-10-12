using CommandLine;
using Serilog;
 
namespace Clood;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Configure and initialize Serilog
        LogConfig.ConfigureLogging();

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