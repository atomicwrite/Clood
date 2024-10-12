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
                return CloodPromptErrorResponse(e);
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
                return CloodStartErrorResponse(e);
            }
        });


        app.MapPost("/api/clood/discard",
            async ([FromBody] DiscardRequest request, [FromSession] CloodSession session) =>
            {
                try
                {
                    Log.Information("Merging Clood changes for request: {@Request}", request);
                    return await DiscardCloodApi.DiscardCloodChanges(request, session);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error occurred while discarding Clood changes");
                    return CloodDiscardErrorResponse(e);
                }
            });
        app.MapPost("/api/clood/merge", async ([FromBody] MergeRequest request) =>
        {
            try
            {
                Log.Information("Merging Clood changes for request: {@Request}", request);
                return await MergeCloodApi.MergeCloodChanges(request);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occurred while merging Clood changes");
                return CloodMergeErrorResponse(e);
            }
        });
        app.MapPost("/api/clood/revert", async ([FromBody] string request) =>
        {
            try
            {
                Log.Information("Reverting Clood changes for request: {Request}", request);
                return await RevertCloodApi.RevertCloodChanges(request);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occurred while reverting Clood changes");
                return CloodMergeRevertResponse(e);
            }
        });
       
        GitRoot = gitRoot;
        Log.Information("API configured with GitRoot: {GitRoot}", GitRoot);
    }

    private static IResult CloodMergeRevertResponse(Exception e)
    {
        Log.Error(e, "CloodMergeRevertResponse error");
        return Results.Ok(new CloodResponse<string>()
        {
            Data = "",
            ErrorMessage = e.Message,
            Success = false
        });
    }

    private static IResult CloodPromptErrorResponse(Exception e)
    {
        Log.Error(e, "CloodPromptErrorResponse error");
        return Results.Ok(new CloodResponse<string>()
        {
            Data = "",
            ErrorMessage = e.Message,
            Success = false
        });
    }

    private static IResult CloodDiscardErrorResponse(Exception e)
    {
        Log.Error(e, "CloodMergeErrorResponse error");
        return Results.Ok(new CloodResponse<string>()
        {
            Data = "",
            ErrorMessage = e.Message,
            Success = false
        });
    }

    private static IResult CloodMergeErrorResponse(Exception e)
    {
        Log.Error(e, "CloodMergeErrorResponse error");
        return Results.Ok(new CloodResponse<string>()
        {
            Data = "",
            ErrorMessage = e.Message,
            Success = false
        });
    }

    private static IResult CloodStartErrorResponse(Exception e)
    {
        Log.Error(e, "CloodStartErrorResponse error");
        return Results.Ok(new CloodResponse<CloodStartResponse>()
        {
            Data = new CloodStartResponse()
            {
                Id = "-1",
                NewBranch = "",
                ProposedChanges = new FileChanges()
                {
                    Answered = false,
                    ChangedFiles = [],
                    NewFiles = [],
                }
            },
            ErrorMessage = e.Message,
            Success = false
        });
    }
}