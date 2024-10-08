using Markdig;
using Markdig.Syntax;
using Newtonsoft.Json;
using Claudia;

namespace Clood;

public static class ClaudiaHelper
{
    public static Dictionary<string, string>? Claudia2Json(string response)
    {
        try
        {
            var jsonContent = ExtractJsonFromMarkdown(response);
            if (string.IsNullOrEmpty(jsonContent))
            {
                Console.WriteLine("Error: No JSON content found in the response.");
                return null;
            }

            var fileContents = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
            return fileContents;
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

    public static bool IsOverloadedError(Exception ex)
    {
        if (ex is ClaudiaException claudiaEx)
        {
            return (int)claudiaEx.Status == 529 && claudiaEx.Name == "overloaded_error";
        }
        return false;
    }

    public static async Task<string?> RetryOnOverloadedError(Func<Task<string?>> action, int maxRetries = 3, int delayMs = 1000)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                if (IsOverloadedError(ex))
                {
                    Console.WriteLine($"Overloaded error detected. Retrying in {delayMs}ms... (Attempt {i + 1}/{maxRetries})");
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
}