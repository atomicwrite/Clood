using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Configuration;
using Claudia;
using Markdig;
using Markdig.Syntax;
using CommandLine;
using Serilog;

namespace Clood;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Configure and initialize Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/clood-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

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