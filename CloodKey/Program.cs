using System;
using CommandLine;
using System.Collections.Generic;

namespace CloodKey
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(RunOptions)
                .WithNotParsed<Options>(HandleParseError);
        }

        static void RunOptions(Options opts)
        {
            var credentialManager = new CredentialManager(opts.AzurePath, opts.AwsPath, opts.WindowsPath);

            switch (opts.Provider?.ToLower())
            {
                case "azure":
                case "aws":
                case "windows":
                    if (opts.Get)
                    {
                        var value = credentialManager.GetCredential(opts.Provider, opts.Key);
                        Console.WriteLine($"Value for {opts.Key}: {value}");
                    }
                    else if (opts.Set)
                    {
                        credentialManager.SetCredential(opts.Provider, opts.Key, opts.Value);
                        Console.WriteLine($"Value set for {opts.Key}");
                    }
                    else
                    {
                        Console.WriteLine("Please specify --get or --set");
                    }
                    break;
                default:
                    Console.WriteLine("Please specify a valid provider: azure, aws, or windows");
                    break;
            }
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine("Invalid arguments. Please check the usage and try again.");
        }
    }

    public class Options
    {
        [Option('p', "provider", Required = true, HelpText = "Credential provider (azure, aws, or windows)")]
        public string? Provider { get; set; }

        [Option('g', "get", Required = false, HelpText = "Get a credential value")]
        public bool Get { get; set; }

        [Option('s', "set", Required = false, HelpText = "Set a credential value")]
        public bool Set { get; set; }

        [Option('k', "key", Required = true, HelpText = "The key of the credential")]
        public string? Key { get; set; }

        [Option('v', "value", Required = false, HelpText = "The value to set (only used with --set)")]
        public string? Value { get; set; }

        [Option("azure-path", Required = false, HelpText = "Path to Azure credentials")]
        public string? AzurePath { get; set; }

        [Option("aws-path", Required = false, HelpText = "Path to AWS credentials")]
        public string? AwsPath { get; set; }

        [Option("windows-path", Required = false, HelpText = "Path to Windows credentials")]
        public string? WindowsPath { get; set; }
    }
}