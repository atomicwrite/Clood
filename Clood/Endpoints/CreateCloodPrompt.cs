using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Clood.Endpoints.API;
using Clood.Endpoints.DTO;
using Clood.Files;
using Clood.Helpers;

namespace Clood.Endpoints;

public static class CreateCloodPrompt
{
    public static async Task<IResult> CreateCloodPromptChanges([FromBody] CloodPromptRequest request)
    {
        Log.Information("Starting CreateCloodPromptChanges method");
        var response = new CloodResponse<PromptImprovement>();

        try
        { 


       


            Log.Information("Sending formatted prompt to Claude AI");

            var claudeResponse = await ClaudiaHelper.SendPromptHelpRequestToClaudia(request.Prompt, CloodApi.GitRoot);

            if (string.IsNullOrWhiteSpace(claudeResponse))
            {
                Log.Warning("Received invalid response from Claude AI");
                response.Success = false;
                response.ErrorMessage = "Invalid response from Claude AI.";
                return Results.Ok(response);
            }

            response.Success = true;

            response.Data = ClaudiaHelper.ClaudiaPrompt2Json(claudeResponse) ??
                            throw new InvalidDataException("could not parse claud json");
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