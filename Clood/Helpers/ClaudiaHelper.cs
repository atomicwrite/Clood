using System.Text;
using Claudia;
using Clood.Files;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;

namespace Clood.Helpers;

public static class ClaudiaHelper
{
    static ClaudiaHelper()
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

        Anthropic = new Anthropic
        {
            ApiKey = apiKey
        };
        MaxTokens = 8192;
    }

    private static readonly Anthropic Anthropic;
    private static readonly int MaxTokens;
    private static readonly ILogger Log = Serilog.Log.ForContext<Program>();

    public static PromptImprovement? ClaudiaPrompt2Json(string response)
    {
        try
        {
            var jsonContent = ClaudiaMarkDownHelper.ExtractJsonFromMarkdown(response);
            if (!string.IsNullOrEmpty(jsonContent))
                return JsonConvert.DeserializeObject<PromptImprovement>(jsonContent);
            Log.Error("No JSON content found in the response.");
            return null;
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
            var jsonContent = ClaudiaMarkDownHelper.ExtractJsonFromMarkdown(response);
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


    public static async Task<string?> SendPromptHelpRequestToClaudia(string prompt, string rootFolder)
    {
        try
        {
            var yamlMap = new CloodFileMap(rootFolder).CreateYamlMap();
            var instruction = ClaudiaHelperPrompts.FormatPromptHelperPrompt(prompt, yamlMap);
            StringBuilder sb = new StringBuilder();
            List<MessageResponse> responses = [];
            var userMessage = new Message() { Role = Roles.User, Content = [instruction] };
            var message = await Anthropic.Messages.CreateAsync(new MessageRequest
            {
                Model = Models.Claude3_5Sonnet,
                MaxTokens = MaxTokens,
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

    public static async Task<string> SendRequestToClaudia(string prompt, string rootFolder,
        List<string> files)
    {
        StringBuilder sb = new StringBuilder();
        var sources = files.Select(f =>
            new Content(File.ReadAllText(f))).ToArray();
        var filesDict = JsonConvert.SerializeObject(files.ToDictionary(a => a, File.ReadAllText));

        var yamlMap = new CloodFileMap(rootFolder).CreateYamlMap();

        var instruction = ClaudiaHelperPrompts.FormatCodeHelperPrompt(filesDict, prompt, rootFolder, yamlMap);
        var userMessage = new Message() { Role = Roles.User, Content = [..sources, instruction, prompt] };

        List<Message> chatMessages = [userMessage];
        List<MessageResponse> responses = new List<MessageResponse>();
        var message = await Anthropic.Messages.CreateAsync(new()
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
        if (string.IsNullOrEmpty(sb.ToString()))
        {
            throw new ClaudiaEmptyResponseException("Received invalid response from Claude AI");
        }

        return sb.ToString();
    }

    private static async Task FinishClaudiaMessage(Message userMessage, StringBuilder sb,
        List<MessageResponse> responses)
    {
        await Task.Delay(TimeSpan.FromSeconds(5));


        var newMessage = await Anthropic.Messages.CreateAsync(new()
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

public class ClaudiaEmptyResponseException(string message) : Exception(message)
{
}