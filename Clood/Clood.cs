using System.Reflection;
using Clood.Endpoints;
using Clood.Helpers;

namespace Clood;

public static class Clood
{
    public static Task RunWithOptions(CliOptions opts)
    {
        if (opts.Version)
        {
            Console.WriteLine($"Clood Program v{Assembly.GetExecutingAssembly().GetName().Version}");
            return Task.CompletedTask;
        }

        ClaudiaHelper.MaxTokens = opts.MaxTokens;
        if (!opts.Server)
            return Task.CompletedTask;
        CloodServer.Start(opts.Urls, opts.GitRoot);
        return Task.CompletedTask;
    }
}