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
            CloodServer.Start( opts.Urls);
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

        Console.WriteLine("Claudia's response:");
        Console.WriteLine((string?)response);

        if (string.IsNullOrWhiteSpace(response))
        {
            Console.WriteLine("No valid response found.");
            return;
        }

        var currentBranch = "";
        try
        {
            currentBranch = await Git.GetCurrentBranch(opts.GitRoot);
            var newBranchName = await Git.CreateNewBranch(opts.GitRoot, files);
            await ApplyChanges(response, files);
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
            .Build();

        var apiKey = config["clood-key"] ?? throw new Exception("Missing clood-key in user secrets.");
        anthropic = new Anthropic
        {
            ApiKey = apiKey
        };
    }

    private static string GetUserPrompt()
    {
        Console.WriteLine("Enter your prompt for Claudia:");
        return Console.ReadLine();
    }

    public static async Task<string?> SendRequestToClaudia(string prompt, string systemPrompt, List<string> files)
    {
        try
        {
            var sources = files.Select(f =>
                new Content(File.ReadAllText(f)));
            var filesDict = JsonConvert.SerializeObject(files.ToDictionary(Path.GetFileName, File.ReadAllText));

            var instruction = $$"""
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

                              4. For each file:
                                 a. Read the file's content
                                 b. Apply the given prompt to the file's content
                                 c. Generate the modified content based on the prompt
                                 

                              5. After processing all files, format your response as a JSON dictionary where:
                                 - The keys are the file names (as listed in step 2)
                                 - The values are the new contents of each modified file


                              6. Ensure that the JSON is properly formatted and escaped, especially for multi-line code contents.

                              Here's an example of how your output should be structured:

                              ```json
                              {
                                "file1.py": "# Modified content of file1.py\n...",
                                "file2.js": "// Modified content of file2.js\n...",
                                "file3.cpp": "// Modified content of file3.cpp\n..."
                              
                              }
                              ```

                              Remember to process all files provided and include them in the final JSON output.
                              """;
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                instruction = systemPrompt;
            }

            instruction += $"""
                           1. You will be given a prompt by the user 
                           <PROMPT>
                           {prompt}
                            </PROMPT>
                            
                           Please provide your response as a JSON dictionary where the keys are the file names 
                           """ +
                           
                           
                           "and the values are the new contents of each modified file. " +
                           $"The files are {string.Join(',', files.Select(Path.GetFileName))}. " +
                           $"They are in the same order as the upload. " +
                           "Only include files in the JSON that you've modified. Format your answer in markdown";


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
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Error Code: {(int)ex.Status} - {ex.Name}");
            return null;
        }
    }

    public static async Task ApplyChanges(string response, List<string> files)
    {
        try
        {
            var jsonContent = ExtractJsonFromMarkdown(response);
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
                var fullPath = files.FirstOrDefault(f => Path.GetFileName(f) == fileName);
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

    private static string ExtractJsonFromMarkdown(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder().Build();
        var document = Markdown.Parse(markdown, pipeline);

        var codeBlocks = document.Descendants<FencedCodeBlock>();
        var jsonBlock = codeBlocks.FirstOrDefault(block =>
            block.Info.Equals("json", StringComparison.OrdinalIgnoreCase));

        if (jsonBlock == null) return string.Empty;
        return string.Join(Environment.NewLine, jsonBlock.Lines.Lines);
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