using Markdig;
using Markdig.Syntax;
using Newtonsoft.Json;
using Claudia;
using Microsoft.Extensions.Configuration;

namespace Clood;



public static class ClaudiaHelper
{
    public static FileChanges? Claudia2Json(string response)
    {
        try
        {
            var jsonContent = ExtractJsonFromMarkdown(response);
            if (string.IsNullOrEmpty(jsonContent))
            {
                Console.WriteLine("Error: No JSON content found in the response.");
                return null;
            }

            var fileChanges = JsonConvert.DeserializeObject<FileChanges>(jsonContent);
            if (fileChanges == null)
                return fileChanges;
            fileChanges.ChangedFiles ??= [];

            fileChanges.NewFiles ??= [];
            return fileChanges;
        }
        catch (JsonException e)
        {
            Console.WriteLine($"JSON parsing error: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected error: {e.Message}");
            return null;
        }
    }

    private static Anthropic anthropic;

    public static void SetupApiKey()
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

    private static bool IsOverloadedError(Exception ex)
    {
        if (ex is ClaudiaException claudiaEx)
        {
            return (int)claudiaEx.Status == 529 && claudiaEx.Name == "overloaded_error";
        }

        return false;
    }

    private static async Task<string?> RetryOnOverloadedError(Func<Task<string?>> action, int maxRetries = 3,
        int delayMs = 1000)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                if (IsOverloadedError(ex))
                {
                    Console.WriteLine(
                        $"Overloaded error detected. Retrying in {delayMs}ms... (Attempt {i + 1}/{maxRetries})");
                    await Task.Delay(delayMs);
                    delayMs *= 2; // Exponential backoff
                }
                else
                {
                    throw; // Re-throw if it's not an overloaded error
                }
            }
        }

        throw new Exception($"Failed after {maxRetries} attempts due to overloaded errors.");
    }

    public static async Task<string?> SendRequestToClaudia(string prompt, string root_folder, string systemPrompt,
        List<string> files)
    {
        return await RetryOnOverloadedError(async () =>
        {
            try
            {
                var sources = files.Select(f =>
                    new Content(File.ReadAllText(f)));
                var filesDict = JsonConvert.SerializeObject(files.ToDictionary(a => a, File.ReadAllText));
             
                var yamlMap = new CloodFileMap(root_folder).CreateYamlMap();
             var   instruction = ClaudiaHelperPrompts.FormatCodeHelperPrompt(filesDict, prompt, root_folder,yamlMap);

                var message = await anthropic.Messages.CreateAsync(new()
                {
                    Model = Models.Claude3_5Sonnet,
                    MaxTokens = 4000,
                    Messages = [new() { Role = Roles.User, Content = [..sources, instruction, prompt] }],
                });

                return message.Content[0].Text;
            }
            catch (ClaudiaException ex)
            {
                if (IsOverloadedError(ex))
                {
                    throw; // This will be caught by RetryOnOverloadedError
                }

                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Error Code: {(int)ex.Status} - {ex.Name}");
                return null;
            }
        });
    }
}