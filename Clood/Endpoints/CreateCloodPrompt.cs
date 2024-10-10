using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text;
using Newtonsoft.Json;

namespace Clood.Endpoints;

public static class CreateCloodPrompt
{
    public static async Task<IResult> CreateCloodPromptChanges([FromBody] CloodPromptRequest request)
    {
        Log.Information("Starting CreateCloodPromptChanges method");
        var response = new CloodResponse<string>();

        try
        {
            // Get the folder layout YAML
           
            var folderLayoutYaml = new CloodFileMap(CloodApi.GitRoot).CreateYamlMap();
            // Get the file contents
            var files = request.Files.Select(a => Path.Join(CloodApi.GitRoot, a)).ToList();
            var filesDict = JsonConvert.SerializeObject(files.ToDictionary(a => a, File.ReadAllText));


            // Format the prompt using FormatPromptHelperPrompt
            var formattedPrompt = ClaudiaHelperPrompts.FormatPromptHelperPrompt(
                filesDict.ToString(),
                request.Prompt,
                folderLayoutYaml
            );

            Log.Information("Sending formatted prompt to Claude AI");
            
            var claudeResponse = await ClaudiaHelper.SendRequestToClaudia(
                formattedPrompt,
                CloodApi.GitRoot,
                "",
                files
            );

            if (string.IsNullOrWhiteSpace(claudeResponse))
            {
                Log.Warning("Received invalid response from Claude AI");
                response.Success = false;
                response.ErrorMessage = "Invalid response from Claude AI.";
                return Results.Ok(response);
            }

            response.Success = true;
            response.Data = claudeResponse;

            Log.Information("Successfully processed prompt with Claude AI");
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in CreateCloodPromptChanges");
            response.Success = false;
            response.ErrorMessage = $"An error occurred: {ex.Message}";
            return Results.Ok(response);
        }
    }
}

public class CloodPromptRequest
{
    public string Prompt { get; set; } = string.Empty; 
    public List<string> Files { get; set; } = new List<string>();
}
