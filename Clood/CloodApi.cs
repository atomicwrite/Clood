using System.Collections.Concurrent;
using CliWrap;
using Clood.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Clood;

public static class CloodApi
{
    public static string GitRoot { get; set; } = null!;

    public static void ConfigureApi(WebApplication app, string gitRoot)
    {
        app.MapPost("/api/clood/prompt", async ([FromBody] CloodPromptRequest request) =>
        {
            try
            {
                Log.Information("Starting Clood changes for request: {@Request}", request);
                return await CreateCloodPrompt.CreateCloodPromptChanges(request);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occurred while starting Clood changes");
                return CloodApiErrorHandlers.CloodPromptErrorResponse(e);
            }
        });
        app.MapPost("/api/clood/start", async ([FromBody] CloodRequest request) =>
        {
            try
            {
                Log.Information("Starting Clood changes for request: {@Request}", request);
                return await CreateCloodApi.CreateCloodChanges(request);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occurred while starting Clood changes");
                return CloodApiErrorHandlers.CloodStartErrorResponse(e);
            }
        });


        app.MapPost("/api/clood/discard",
            async ([FromBody] DiscardRequest request, [FromSession] CloodSession session) =>
            {
                try
                {
                    Log.Information("Merging Clood changes for request: {@Request}", request);
                    if (session is not { UseGit: false })
                        return await DiscardCloodApi.DiscardCloodChanges(request, session);

                    var response = new CloodResponse<string>();
                    Log.Information("Session {SessionId} does not use Git. No changes to apply.", request.Id);
                    response.Success = true;
                    response.Data = "Session closed. No changes were applied.";
                    return Results.Ok(response);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error occurred while discarding Clood changes");
                    return CloodApiErrorHandlers.CloodDiscardErrorResponse(e);
                }
            });
        app.MapPost("/api/clood/merge", async ([FromBody] MergeRequest request, [FromSession] CloodSession session) =>
        {
            try
            {
                Log.Information("Merging Clood changes for request: {@Request}", request);
                return await MergeCloodApi.MergeCloodChanges(request, session);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occurred while merging Clood changes");
                return CloodApiErrorHandlers.CloodMergeErrorResponse(e);
            }
        });
        app.MapPost("/api/clood/revert", async ([FromSession] CloodSession session) =>
        {
            try
            {
                Log.Information("Reverting Clood changes for request: {Request}", session);
                return await RevertCloodApi.RevertCloodChanges(session);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occurred while reverting Clood changes");
                return CloodApiErrorHandlers.CloodMergeRevertResponse(e);
            }
        });

        GitRoot = gitRoot;
        Log.Information("API configured with GitRoot: {GitRoot}", GitRoot);
    }
}