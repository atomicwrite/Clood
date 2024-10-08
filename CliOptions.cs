using CommandLine;
using System.IO;
using System.Linq;

namespace Clood;

public class CliOptions
{
    [Option('v', "version", Required = false, HelpText = "Print the version and quit")]
    public bool Version { get; set; }
    
    [Option('g', "git-path", Required = false, HelpText = "Optional path to git")]
    public string? GitPath { get; set; }
    
    [Option('r', "git-root", Required = true, HelpText = "The Git root ")]
    public string GitRoot { get; set; }
    
    [Option('p', "prompt", Required = true, HelpText = "Prompt for Claude AI.")]
    public string Prompt { get; set; }

    [Option('s', "system-prompt", Required = false, HelpText = "c:\\sysprompt.md")]
    public string SystemPrompt { get; set; }

    [Value(0, Min = 1, HelpText = "List of files to process.")]
    public IEnumerable<string> Files { get; set; }

    public bool Validate()
    {
        bool isValid = true;

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

        // Check if all specified files exist
        var nonExistentFiles = Files.Where(file => !File.Exists(file)).ToList();
        if (nonExistentFiles.Count == 0) return isValid;
        {
            Console.WriteLine("Error: The following specified files do not exist:");
            foreach (var file in nonExistentFiles)
            {
                Console.WriteLine($"  - {file}");
            }
            isValid = false;
        }

        return isValid;
    }
}