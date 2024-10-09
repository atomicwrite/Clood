using System;
using CommandLine;
using System.Collections.Generic;
using CloodKey.Interfaces;
 
using CloodKey.Options;

namespace CloodKey
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
        }

        static void RunOptions(CommandLineOptions opts)
        {
            // try
            // {
            //     IKeyCLI keyCLI = opts.CredentialStore.ToLower() switch
            //     {
            //         "aws" => new AwsKeyCLI(),
            //         "azure" => new AzureKeyCLI(),
            //         "os" => new OsKeyCLI(),
            //         _ => throw new ArgumentException("Invalid credential store specified.")
            //     };
            //
            //     if (!keyCLI.ValidateCliTool())
            //     {
            //         Console.WriteLine($"The CLI tool for {opts.CredentialStore} is not installed or not found in the system PATH.");
            //         return;
            //     }
            //
            //     string result = opts.Operation.ToLower() switch
            //     {
            //         "get" => keyCLI.Get(opts.Key),
            //         "set" => keyCLI.Set(opts.Key, opts.Value),
            //         _ => throw new ArgumentException("Invalid operation specified.")
            //     };
            //
            //     Console.WriteLine(result);
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine($"An error occurred: {ex.Message}");
            // }
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine("Invalid command line arguments. Please check the usage and try again.");
        }
    }
}
