using Markdig;
using Markdig.Syntax;
using Newtonsoft.Json;

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
            return null;
        }
        catch (Exception e)
        {
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
}