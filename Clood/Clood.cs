using System.Reflection;
using Claudia;
using CliWrap;
using CliWrap.Buffered;
using Markdig;
using Markdig.Syntax;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Clood;

public static class Clood
{
    private static Anthropic anthropic;

    public static async Task RunWithOptions(CliOptions opts)
    {
        if (opts.Version)
        {
            Console.WriteLine("Clood Program v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            return;
        }

        SetupApiKey();
        if (opts.Server)
        {
            CloodServer.Start(opts.Urls);
        }

        if (!string.IsNullOrEmpty(opts.GitPath))
        {
            Git.PathToGit = opts.GitPath;
        }

        Console.WriteLine("Welcome to the Claudia API and Git Integration Script!");


        var files = opts.Files.ToList();
        if (files.Count == 0)
        {
            Console.WriteLine("No files were specified.");
            return;
        }


        Console.WriteLine("Files we are working on:");
        foreach (var file in files)
        {
            Console.WriteLine($"- {file}");
        }

        // Check for uncommitted changes
        if (await HasUncommittedChanges(opts.GitRoot))
        {
            Console.WriteLine("Warning: You have uncommitted changes in your working directory.");
            if (await GitHelpers.AskToCommitUncommited(opts.GitRoot))
                return;
        }

        var prompt = opts.Prompt;
        var systemPrompt = await GetSystemPrompt(opts);

        var response = await SendRequestToClaudia(prompt, systemPrompt, files);

        if (string.IsNullOrWhiteSpace(response))
        {
            Console.WriteLine("No valid response found.");
            return;
        }

        var fileChanges = ClaudiaHelper.Claudia2Json(response);
        if (fileChanges == null)
        {
            Console.WriteLine($"No valid response from Claude AI. {response}");
            return;
        }

        var currentBranch = "";
        try
        {
            currentBranch = await Git.GetCurrentBranch(opts.GitRoot);
            var newBranchName = await Git.CreateNewBranch(opts.GitRoot, files);
            var fileContents = ClaudiaHelper.Claudia2Json(response) ??
                               throw new JsonException($"Response from claude was invalid json {response}");

    
            await ApplyChanges(fileChanges, opts.GitRoot);
            GitHelpers.AskToOpenFiles(files);
            await GitHelpers.AskToKeepChanges(opts.GitRoot, currentBranch, newBranchName);


            Console.WriteLine("Script execution completed.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
            await GitHelpers.AskToAbandonChanges(opts.GitRoot, currentBranch);
        }
    }

    private static async Task<string> GetSystemPrompt(CliOptions opts)
    {
        var systemPrompt = "";
        if (string.IsNullOrEmpty(opts.SystemPrompt)) return systemPrompt;
        if (File.Exists(opts.SystemPrompt))
        {
            systemPrompt = await File.ReadAllTextAsync(opts.SystemPrompt);
        }

        return systemPrompt;
    }

    private static void SetupApiKey()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        var apiKey = config["clood-key"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("Missing clood-key in user secrets or environment variables.");
        }

        if (Environment.GetEnvironmentVariable("clood-key") != null)
        {
            Console.WriteLine("Warning: Using clood-key from environment variable is unsafe.");
        }

        anthropic = new Anthropic
        {
            ApiKey = apiKey
        };
    }

    public static async Task<string?> SendRequestToClaudia(string prompt, string systemPrompt, List<string> files)
    {
        return await ClaudiaHelper.RetryOnOverloadedError(async () =>
        {
            try
            {
                var sources = files.Select(f =>
                    new Content(File.ReadAllText(f)));
                var filesDict = JsonConvert.SerializeObject(files.ToDictionary(a => a, File.ReadAllText));
                var instruction = "";
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    instruction = systemPrompt;
                }

                instruction = $$"""
                                You are tasked with applying a specific prompt to multiple code files and returning the modified contents in a JSON format. Here's how to proceed:

                                1. You will be given a dictionary where the filename is the key and the value is the content, a prompt to apply, and the contents of multiple code files.

                                2. The file dictionary is as follows:
                                <file_dictionary>
                                {{filesDict}}
                                 </file_dictionary>

                                3. The prompt to apply to each file is:
                                <prompt>
                                {{prompt}}
                                </prompt>

                                4. Read all the files, some may not need to be changed and are just there for context:
                                  a. Generate the modified content based on the prompt
                                  b. If new files need to be created, include them in the output
                                
                                5. After processing all files, format your response as a JSON object with two arrays:
                                   - "changedFiles": An array of objects, each containing "filename" and "content" for modified existing files
                                   - "newFiles": An array of objects, each containing "filename" and "content" for newly created files

                                6. Ensure that the JSON is properly formatted and escaped, especially for multi-line code contents.

                                Here's an example of how your output should be structured:

                                ```json
                                {
                                  "changedFiles": [
                                    {"filename": "file1.py", "content": "# Modified content of file1.py\n..."},
                                    {"filename": "file2.js", "content": "// Modified content of file2.js\n..."}
                                  ],
                                  "newFiles": [
                                    {"filename": "newfile.cpp", "content": "// Content of new file\n..."}
                                  ]
                                }
                                ```

                                Remember to include all modified files in the "changedFiles" array and any new files in the "newFiles" array.
                                """;

                var message = await anthropic.Messages.CreateAsync(new()
                {
                    Model = Models.Claude3_5Sonnet,
                    MaxTokens = 4000,
                    Messages = [new() { Role = Roles.User, Content = [..sources, instruction, prompt] }]
                });

                return message.Content[0].Text;
            }
            catch (ClaudiaException ex)
            {
                if (ClaudiaHelper.IsOverloadedError(ex))
                {
                    throw; // This will be caught by RetryOnOverloadedError
                }

                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Error Code: {(int)ex.Status} - {ex.Name}");
                return null;
            }
        });
    }

    public static async Task ApplyChanges(FileChanges fileChanges, string gitRoot)
    {
        // Handle changed files
        foreach (var file in fileChanges.ChangedFiles)
        {
            string fullPath = GetFullPath(file.Filename, gitRoot);
            if (fullPath == null)
            {
                Console.WriteLine($"Warning: Skipping changed file outside git root: {file.Filename}");
                continue;
            }

            await File.WriteAllTextAsync(fullPath, file.Content);
            Console.WriteLine($"Updated file: {file.Filename}");
        }

        // Handle new files
        foreach (var file in fileChanges.NewFiles)
        {
            string fullPath = GetFullPath(file.Filename, gitRoot);
            if (fullPath == null)
            {
                Console.WriteLine($"Warning: Skipping new file outside git root: {file.Filename}");
                continue;
            }

            if (File.Exists(fullPath))
            {
                throw new IOException($"Error: New file already exists: {file.Filename}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            await File.WriteAllTextAsync(fullPath, file.Content);
            Console.WriteLine($"Created new file: {file.Filename}");
        }
    }

    private static string GetFullPath(string filename, string gitRoot)
    {
        string fullPath;
        if (Path.IsPathRooted(filename))
        {
            // If the path is absolute, ensure it starts with the git root
            if (!filename.StartsWith(gitRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null; // File is outside git root
            }
            fullPath = filename;
        }
        else
        {
            // If the path is relative, combine it with the git root
            fullPath = Path.GetFullPath(Path.Combine(gitRoot, filename));
        
            // Ensure the resulting path is still within the git root
            if (!fullPath.StartsWith(gitRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null; // File is outside git root
            }
        }
        return fullPath;
    }

    public static async Task<bool> HasUncommittedChanges(string gitRoot)
    {
        try
        {
            var result = await Cli.Wrap("git")
                .WithArguments("status --porcelain")
                .WithWorkingDirectory(gitRoot)
                .ExecuteBufferedAsync();

            return !string.IsNullOrWhiteSpace(result.StandardOutput);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking for uncommitted changes: {ex.Message}");
            return false;
        }
    }
}