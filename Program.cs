using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Configuration;
using Claudia;
using Markdig;
using Markdig.Syntax;

namespace Clood;

class Program
{
    private static Anthropic _anthropic;
    private static readonly List<string> Files = new List<string>();
    private static string _workingDirectory;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Welcome to the Claudia API and Git Integration Script!");

        SetupApiKey();
        await HandleFileInput(args);
        if (Files.Count == 0)
        {
            return;
        }

        // Check for uncommitted changes
        if (await HasUncommittedChanges())
        {
            Console.WriteLine("Warning: You have uncommitted changes in your working directory.");
            Console.Write("Do you want to commit these changes before proceeding? (y/n): ");
            if (Console.ReadLine().ToLower() == "y")
            {
                Console.Write("Enter a commit message: ");
                string commitMessage = Console.ReadLine();
                await Git.CommitChanges(_workingDirectory, commitMessage);
                Console.WriteLine("Changes committed successfully.");
            }
            else
            {
                Console.Write(
                    "Proceeding with uncommitted changes might affect the script's behavior. Do you want to exit? (y/n): ");
                if (Console.ReadLine().ToLower() == "y")
                {
                    Console.WriteLine("Exiting script. No changes were made.");
                    return;
                }

                Console.WriteLine("Proceeding with uncommitted changes. Please be cautious.");
            }
        }

        var prompt = GetUserPrompt();
        var response = await SendRequestToClaudia(prompt);

        Console.WriteLine("Claudia's response:");
        Console.WriteLine(response);

        if (string.IsNullOrWhiteSpace(response))
        {
            Console.WriteLine("No valid response found.");
            return;
        }

        var currentBranch = "";
        try
        {
            currentBranch = await Git.GetCurrentBranch(_workingDirectory);
            var newBranchName = await Git.CreateNewBranch(_workingDirectory, Files);
            await ApplyChanges(response);
            AskToOpenFiles();

            if (AskToKeepChanges())
            {
                await Git.CommitChanges(_workingDirectory, "Changes made by Claudia AI");
                await Git.MergeChanges(_workingDirectory, currentBranch, newBranchName);
            }
            else
            {
                await Git.SwitchToBranch(_workingDirectory, currentBranch);
                await Git.DeleteBranch(_workingDirectory, newBranchName);
            }

            Console.WriteLine("Script execution completed.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
            Console.Write("Do you want to abandon changes? (y/n): ");
            if (Console.ReadLine().ToLower() == "y")
            {
                await Git.SwitchToBranch(_workingDirectory, currentBranch);
                Console.WriteLine("Changes abandoned. Switched back to the original branch.");
            }
            else
            {
                Console.WriteLine("Attempting to merge changes despite the error...");
                try
                {
                    var currentBranchName = await Git.GetCurrentBranch(_workingDirectory);
                    await Git.CommitChanges(_workingDirectory, "Changes made by Claudia AI (with errors)");
                    await Git.MergeChanges(_workingDirectory, currentBranch, currentBranchName);
                    Console.WriteLine("Changes merged successfully.");
                }
                catch (Exception mergeEx)
                {
                    Console.WriteLine($"Error merging changes: {mergeEx.Message}");
                    Console.WriteLine("You may need to resolve conflicts manually.");
                }
            }
        }
    }

    static async Task<bool> HasUncommittedChanges()
    {
        try
        {
            var result = await Cli.Wrap("git")
                .WithArguments("status --porcelain")
                .WithWorkingDirectory(_workingDirectory)
                .ExecuteBufferedAsync();

            return !string.IsNullOrWhiteSpace(result.StandardOutput);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking for uncommitted changes: {ex.Message}");
            return false;
        }
    }

    static void SetupApiKey()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        string apiKey = config["clood-key"] ?? throw new Exception("Missing clood-key in user secrets.");
        _anthropic = new Anthropic
        {
            ApiKey = apiKey
        };
    }

    static async Task HandleFileInput(string[] args)
    {
        if (args.Length > 0)
        {
            Files.AddRange(args);
            Console.WriteLine("Files we are working on:");
            foreach (var file in Files)
            {
                Console.WriteLine($"- {file}");
            }

            _workingDirectory = Path.GetDirectoryName(Files[0]);
        }
        else
        {
            Console.WriteLine("No files were specified.");
        }
    }

    static string GetUserPrompt()
    {
        Console.WriteLine("Enter your prompt for Claudia:");
        return Console.ReadLine();
    }

    static async Task<string?> SendRequestToClaudia(string prompt)
    {
        try
        {
            var sources = Files.Select(f =>
                new Content(File.ReadAllText(f)));

            var instruction = "Please provide your response as a JSON dictionary where the keys are the file names " +
                              "and the values are the new contents of each modified file. " +
                              $"The files are {string.Join(',', Files.Select(Path.GetFileName))}. They are in the same order as the upload." +
                              "Only include files in the JSON that you've modified.";

            var message = await _anthropic.Messages.CreateAsync(new()
            {
                Model = Models.Claude3_5Sonnet,
                MaxTokens = 4000,
                Messages =
                [
                    new()
                    {
                        Role = Roles.User,
                        Content =
                        [
                            ..sources,
                            instruction,
                            prompt
                        ]
                    }
                ]
            });

            return message.Content[0].Text;
        }
        catch (ClaudiaException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Error Code: {(int)ex.Status} - {ex.Name}");
            return null;
        }
    }


    static async Task ApplyChanges(string response)
    {
        try
        {
            string jsonContent = ExtractJsonFromMarkdown(response);
            if (string.IsNullOrEmpty(jsonContent))
            {
                Console.WriteLine("Error: No JSON content found in the response.");
                return;
            }

            var fileContents = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
            if (fileContents == null)
            {
                Console.WriteLine("Error: Unable to parse the JSON response from Claude.");
                return;
            }

            foreach (var (fileName, content) in fileContents)
            {
                var fullPath = Files.FirstOrDefault(f => Path.GetFileName(f) == fileName);
                if (fullPath == null)
                {
                    Console.WriteLine($"Warning: File '{fileName}' not found in the provided files list.");
                    continue;
                }

                await File.WriteAllTextAsync(fullPath, content);
                Console.WriteLine($"Updated file: {fileName}");
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing JSON response: {ex.Message}");
            Console.WriteLine("Raw response:");
            Console.WriteLine(response);
        }
    }

    static string ExtractJsonFromMarkdown(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder().Build();
        var document = Markdown.Parse(markdown, pipeline);

        var codeBlocks = document.Descendants<FencedCodeBlock>();
        var jsonBlock = codeBlocks.FirstOrDefault(block =>
            block.Info.Equals("json", StringComparison.OrdinalIgnoreCase));

        if (jsonBlock == null) return string.Empty;
        return string.Join(Environment.NewLine, jsonBlock.Lines.Lines);
    }

    static Dictionary<string, string> ExtractFileContentsFromResponse(string response)
    {
        var fileContents = new Dictionary<string, string>();
        var lines = response.Split('\n');
        string currentFileName = null;
        var currentContent = new List<string>();
        bool inCodeBlock = false;

        foreach (var line in lines)
        {
            if (!inCodeBlock && !line.StartsWith("```"))
            {
                currentFileName = line.Trim();
            }
            else if (line.StartsWith("```"))
            {
                if (inCodeBlock)
                {
                    // End of code block
                    fileContents[currentFileName] = string.Join("\n", currentContent);
                    currentFileName = null;
                    currentContent.Clear();
                }

                inCodeBlock = !inCodeBlock;
            }
            else if (inCodeBlock)
            {
                currentContent.Add(line);
            }
        }

        return fileContents;
    }

    static void AskToOpenFiles()
    {
        Console.Write("Do you want to open the modified files in your current editor? (y/n): ");
        if (Console.ReadLine().ToLower() == "y")
        {
            foreach (var file in Files)
            {
                Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            }
        }
    }

    static bool AskToKeepChanges()
    {
        Console.Write("Do you want to keep the changes? (y/n): ");
        return Console.ReadLine().ToLower() == "y";
    }
}