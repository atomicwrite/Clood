using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clood;

public static class CloodApi
{
    private static readonly ConcurrentDictionary<string, CloodSession> Sessions = new();

    public static void ConfigureApi(WebApplication app)
    {
        app.MapPost("/api/clood/start", StartCloodProcess);
        app.MapPost("/api/clood/merge", MergeCloodChanges);
        app.MapPost("/api/clood/revert", RevertCloodChanges);
    }
    private static async Task<IResult> RevertCloodChanges([FromBody] string sessionId)
    {
        if (!Sessions.TryRemove(sessionId, out var session))
        {
            return Results.NotFound("Session not found.");
        }

        try
        {
            // Switch back to the original branch
            await Git.SwitchToBranch(session.GitRoot, session.OriginalBranch);

            // Delete the temporary branch created for Claude's changes
            await Git.DeleteBranch(session.GitRoot, session.NewBranch);

         
            return Results.Ok("Changes reverted successfully. Returned to original state.");
        }
        catch (Exception ex)
        {
            return Results.BadRequest($"Error during revert process: {ex.Message}");
        }
    }
    private static async Task<IResult> StartCloodProcess([FromBody] CloodRequest request)
    {
        // Check for uncommitted changes
        if (await Clood.HasUncommittedChanges(request.GitRoot))
        {
            return Results.BadRequest("There are uncommitted changes in the repository. Please commit or stash them before proceeding.");
        }

        var currentBranch = await Git.GetCurrentBranch(request.GitRoot);
        var newBranch = await Git.CreateNewBranch(request.GitRoot, request.Files);

        var response = await Clood.SendRequestToClaudia(request.Prompt, request.SystemPrompt, request.Files);
        
        if (string.IsNullOrWhiteSpace(response))
        {
            await Git.DeleteBranch(request.GitRoot, newBranch);
            return Results.BadRequest("No valid response from Claude AI.");
        }

        await Clood.ApplyChanges(response, request.Files);

        var sessionId = Guid.NewGuid().ToString();
        var session = new CloodSession
        {
            OriginalBranch = currentBranch,
            NewBranch = newBranch,
            GitRoot = request.GitRoot,
            Files = request.Files
        };

        if (!Sessions.TryAdd(sessionId, session))
        {
            await Git.DeleteBranch(request.GitRoot, newBranch);
            return Results.StatusCode(500);
        }

        return Results.Ok(new CloodResponse { Id = sessionId, NewBranch = newBranch });
    }

    private static async Task<IResult> MergeCloodChanges([FromBody] MergeRequest request)
    {
        if (!Sessions.TryRemove(request.Id, out var session))
        {
            return Results.NotFound("Session not found.");
        }

        try
        {
            if (request.Merge)
            {
                await Git.MergeChanges(session.GitRoot, session.OriginalBranch, session.NewBranch);
                return Results.Ok("Changes merged successfully.");
            }
            else
            {
                await Git.SwitchToBranch(session.GitRoot, session.OriginalBranch);
                await Git.DeleteBranch(session.GitRoot, session.NewBranch);
                return Results.Ok("Changes discarded.");
            }
        }
        catch (Exception ex)
        {
            return Results.BadRequest($"Error during merge/discard process: {ex.Message}");
        }
    }
}