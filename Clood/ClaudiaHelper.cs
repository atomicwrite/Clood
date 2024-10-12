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
        MaxTokens = 8192;
    }

    private static readonly int MaxTokens;
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


    public static async Task<string?> SendPromptHelpRequestToClaudia(string prompt, string rootFolder)
    {
        try
        {
            var yamlMap = new CloodFileMap(rootFolder).CreateYamlMap();
            var instruction = ClaudiaHelperPrompts.FormatPromptHelperPrompt(prompt, yamlMap);
            StringBuilder sb = new StringBuilder();
            List<MessageResponse> responses = new List<MessageResponse>();
            var userMessage = new Message() { Role = Roles.User, Content = [instruction] };
            var message = await anthropic.Messages.CreateAsync(new()
            {
                Model = Models.Claude3_5Sonnet,
                MaxTokens = 4000,
                Messages = [userMessage],
            });
            responses.Add(message);
            sb.Append(message.Content[0].Text);
            if (message.StopReason == "max_tokens")
            {
                await FinishClaudiaMessage(userMessage, sb, responses);
            }

            return sb.ToString();
        }
        catch (ClaudiaException ex)
        {
            Log.Error(ex, "Error in SendPromptHelpRequestToClaudia. Error Code: {StatusCode} - {ErrorName}",
                (int)ex.Status, ex.Name);
            return null;
        }
    }

    public static async Task<string?> SendRequestToClaudia(string prompt, string root_folder, string systemPrompt,
        List<string> files)
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
                MaxTokens = MaxTokens,
                Messages = chatMessages.ToArray(),
            });
            responses.Add(message);
            sb.Append(message.Content[0].Text);
            if (message.StopReason == "max_tokens")
            {
                await FinishClaudiaMessage(userMessage, sb, responses);
            }

            responses.Clear(); // in case we need to do something later we store in  list for now

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
    }

    private static async Task FinishClaudiaMessage(Message userMessage, StringBuilder sb,
        List<MessageResponse> responses)
    {
        await Task.Delay(TimeSpan.FromSeconds(5));


        var newMessage = await anthropic.Messages.CreateAsync(new()
        {
            Model = Models.Claude3_5Sonnet,
            MaxTokens = MaxTokens,
            Messages = [userMessage, new Message() { Role = Roles.Assistant, Content = sb.ToString() }]
        });
        sb.Append(newMessage.Content[0].Text);
        responses.Add(newMessage);


        if (newMessage.StopReason == "max_tokens")
        {
            await FinishClaudiaMessage(userMessage, sb, responses);
        }
    }
}