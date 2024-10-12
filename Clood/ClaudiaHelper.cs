using System.Text;
using Markdig;
using Markdig.Syntax;
using Newtonsoft.Json;
using Claudia;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Clood;

public static class ClaudiaHelper
{
    static ClaudiaHelper()
    {
        SetupApiKey();
    }

    private static readonly ILogger Log = Serilog.Log.ForContext<Program>();

    public static PromptImprovement? ClaudiaPrompt2Json(string response)
    {
        try
        {
            var jsonContent = ExtractJsonFromMarkdown(response);
            if (string.IsNullOrEmpty(jsonContent))
            {
                Log.Error("No JSON content found in the response.");
                return null;
            }

            return JsonConvert.DeserializeObject<PromptImprovement>(jsonContent);
        }
        catch (JsonException e)
        {
            Log.Error(e, "JSON parsing ClaudiaPrompt2Json error");
            return null;
        }
        catch (Exception e)
        {
            Log.Error(e, "Unexpected ClaudiaPrompt2Json error");
            return null;
        }
    }

    public static FileChanges? Claudia2Json(string response)
    {
        try
        {
            var jsonContent = ExtractJsonFromMarkdown(response);
            if (string.IsNullOrEmpty(jsonContent))
            {
                Log.Error("Claudia2Json: No JSON content found in the response.");
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
            Log.Error(e, "JSON Claudia2Json parsing error");
            return null;
        }
        catch (Exception e)
        {
            Log.Error(e, "Unexpected Claudia2Json error");
            return null;
        }
    }

    private static Anthropic anthropic;

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
            Log.Warning("Using clood-key from environment variable is unsafe.");
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
                    Log.Warning(
                        "Overloaded error detected. Retrying in {DelayMs}ms... (Attempt {Attempt}/{MaxRetries})",
                        delayMs, i + 1, maxRetries);
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

    public static async Task<string?> SendPromptHelpRequestToClaudia(string prompt, string root_folder)
    {
        return await RetryOnOverloadedError(async () =>
        {
            try
            {
                var yamlMap = new CloodFileMap(root_folder).CreateYamlMap();
                var instruction = ClaudiaHelperPrompts.FormatPromptHelperPrompt(prompt, yamlMap);

                var message = await anthropic.Messages.CreateAsync(new()
                {
                    Model = Models.Claude3_5Sonnet,
                    MaxTokens = 4000,
                    Messages = [new() { Role = Roles.User, Content = [instruction] }],
                });

                return message.Content[0].Text;
            }
            catch (ClaudiaException ex)
            {
                if (IsOverloadedError(ex))
                {
                    throw; // This will be caught by RetryOnOverloadedError
                }

                Log.Error(ex, "Error in SendPromptHelpRequestToClaudia. Error Code: {StatusCode} - {ErrorName}",
                    (int)ex.Status, ex.Name);
                return null;
            }
        });
    }

    public static async Task<string?> SendRequestToClaudia(string prompt, string root_folder, string systemPrompt,
        List<string> files)
    {
        return await RetryOnOverloadedError(async () =>
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                var sources = files.Select(f =>
                    new Content(File.ReadAllText(f))).ToArray();
                var filesDict = JsonConvert.SerializeObject(files.ToDictionary(a => a, File.ReadAllText));

                var yamlMap = new CloodFileMap(root_folder).CreateYamlMap();

                var instruction = ClaudiaHelperPrompts.FormatCodeHelperPrompt(filesDict, prompt, root_folder, yamlMap);
                var userMessage = new Message() { Role = Roles.User, Content = [..sources, instruction, prompt] };
                List<Message> chatMessages = [userMessage];
                List<MessageResponse> responses = new List<MessageResponse>();
                var message = await anthropic.Messages.CreateAsync(new()
                {
                    Model = Models.Claude3_5Sonnet,
                    MaxTokens = 8192,
                    Messages = chatMessages.ToArray(),
                });
                responses.Add(message);
                sb.Append(message.Content[0].Text);
                while (message.StopReason == "max_tokens")
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    try
                    {
                        message = await anthropic.Messages.CreateAsync(new()
                        {
                            Model = Models.Claude3_5Sonnet,
                            MaxTokens = 8192,
                            Messages = [userMessage, new Message() { Role = Roles.Assistant, Content = sb.ToString() }]
                        });
                        sb.Append(message.Content[0].Text);
                        responses.Add(message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }

                return sb.ToString();
            }
            catch (ClaudiaException ex)
            {
                if (IsOverloadedError(ex))
                {
                    throw; // This will be caught by RetryOnOverloadedError
                }

                Log.Error(ex, "Error in SendRequestToClaudia. Error Code: {StatusCode} - {ErrorName}", (int)ex.Status,
                    ex.Name);
                return null;
            }
        });
    }
}