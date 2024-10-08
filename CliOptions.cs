using CommandLine;
using System.IO;
using System.Linq;

namespace Clood;

public class CliOptions
{
    [Option('m', "server", Required = false, HelpText = "start a server")]
    public bool Server { get; set; }
    
    [Option('u', "server-urls", Required = false, HelpText = "Minimal api run urls")]
    public string Urls { get; set; }
    
    [Option('v', "version", Required = false, HelpText = "Print the version and quit")]
    public bool Version { get; set; }
    
    [Option('g', "git-path", Required = false, HelpText = "Optional path to git")]
    public string? GitPath { get; set; }
    
    [Option('r', "git-root", Required = true, HelpText = "The Git root ")]
    public string GitRoot { get; set; }
    
    [Option('p', "prompt", Required = false, HelpText = "Prompt for Claude AI.")]
    public string Prompt { get; set; }

    [Option('s', "system-prompt", Required = false, HelpText = "c:\\sysprompt.md")]
    public string SystemPrompt { get; set; }

    [Option("set-api-key", Required = false, HelpText = "Set the API key")]
    public string SetApiKey { get; set; }
    [Option("use-windows-credential", Required = false, HelpText = "Use Windows Credential Manager for storing/retrieving the API key")]
    public bool UseWindowsCredential { get; set; }

    [Option("check-api-key", Required = false, HelpText = "Check the stored API key", Default = false)]
    public bool CheckApiKey { get; set; }

    [Option("aws-secret-name", Required = false, HelpText = "AWS Secrets Manager secret name")]
    public string AwsSecretName { get; set; }

    [Option("aws-region", Required = false, HelpText = "AWS region for Secrets Manager")]
    public string AwsRegion { get; set; }

    [Option("azure-key-vault", Required = false, HelpText = "Azure Key Vault name")]
    public string AzureKeyVaultName { get; set; }

    [Option("azure-secret-name", Required = false, HelpText = "Azure Key Vault secret name")]
    public string AzureSecretName { get; set; }

    [Value(0, Min = 1, HelpText = "List of files to process.")]
    public IEnumerable<string> Files { get; set; }

    public bool Validate()
    {
        bool isValid = true;
        if (Server)
        {
            return true;
        }
        if (UseWindowsCredential && (
                !string.IsNullOrEmpty(AwsSecretName) || 
                !string.IsNullOrEmpty(AzureKeyVaultName) || 
                !string.IsNullOrEmpty(SetApiKey)))
        {
            Console.WriteLine("Error: Cannot use Windows Credential Manager with other key storage options.");
            isValid = false;
        }

        // Check if GitPath exists (if specified)
        if (!string.IsNullOrEmpty(GitPath) && !File.Exists(GitPath))
        {
            Console.WriteLine($"Error: Specified Git path does not exist: {GitPath}");
            isValid = false;
        }
        if (!string.IsNullOrEmpty(SystemPrompt) && !File.Exists(SystemPrompt))
        {
            Console.WriteLine($"Error: Specified SystemPrompt path does not exist: {SystemPrompt}");
            isValid = false;
        }
        if (string.IsNullOrEmpty(Prompt))
        {
            Console.WriteLine($"Error: Prompt was not provided: ");
            isValid = false;
        }

        // Check if all specified files exist
        var nonExistentFiles = Files.Where(file => !File.Exists(file)).ToList();
        if (nonExistentFiles.Count > 0)
        {
            Console.WriteLine("Error: The following specified files do not exist:");
            foreach (var file in nonExistentFiles)
            {
                Console.WriteLine($"  - {file}");
            }
            isValid = false;
        }

        // Validate set-api-key and check-api-key options
        if (!string.IsNullOrEmpty(SetApiKey) && CheckApiKey)
        {
            Console.WriteLine("Error: Cannot set and check API key at the same time.");
            isValid = false;
        }

        // Validate AWS options
        if (!string.IsNullOrEmpty(AwsSecretName) && string.IsNullOrEmpty(AwsRegion))
        {
            Console.WriteLine("Error: AWS region must be specified when using AWS Secrets Manager.");
            isValid = false;
        }

        // Validate Azure options
        if (!string.IsNullOrEmpty(AzureKeyVaultName) && string.IsNullOrEmpty(AzureSecretName))
        {
            Console.WriteLine("Error: Azure secret name must be specified when using Azure Key Vault.");
            isValid = false;
        }

        return isValid;
    }
}