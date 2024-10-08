using System.Collections.Concurrent;
using CliWrap;
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

        if (!session.UseGit)
        {
            // For non-Git sessions, we don't need to do anything as changes weren't actually applied
            return Results.Ok("Session removed. No changes were applied to revert.");
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
        string sessionId = Guid.NewGuid().ToString();
        CloodSession session = new CloodSession
        {
            UseGit = request.UseGit,
            GitRoot = request.GitRoot,
            Files = request.Files
        };

        if (request.UseGit)
        {
            // Check for uncommitted changes
            if (await Clood.HasUncommittedChanges(request.GitRoot))
            {
                return Results.BadRequest(
                    "There are uncommitted changes in the repository. Please commit or stash them before proceeding.");
            }

            session.OriginalBranch = await Git.GetCurrentBranch(request.GitRoot);
            session.NewBranch = await Git.CreateNewBranch(request.GitRoot, request.Files);
        }

        var response = await Clood.SendRequestToClaudia(request.Prompt, request.SystemPrompt, request.Files);

        if (string.IsNullOrWhiteSpace(response))
        {
            if (request.UseGit)
            {
                await Git.DeleteBranch(request.GitRoot, session.NewBranch);
            }

            return Results.BadRequest("No valid response from Claude AI.");
        }

        var fileChanges = ClaudiaHelper.Claudia2Json(response);
        if (fileChanges == null)
        {
            return Results.BadRequest($"No valid response from Claude AI. {response} ");
        }

        session.ProposedChanges = fileChanges;

        if (request.UseGit)
        {
            await Clood.ApplyChanges(fileChanges, request.GitRoot);
        }

        if (!Sessions.TryAdd(sessionId, session))
        {
            if (request.UseGit)
            {
                await Git.DeleteBranch(request.GitRoot, session.NewBranch);
            }

            return Results.StatusCode(500);
        }

        return Results.Ok(new CloodResponse
        {
            Id = sessionId,
            NewBranch = session.NewBranch,
            ProposedChanges = session.ProposedChanges
        });
    }

    private static async Task<IResult> MergeCloodChanges([FromBody] MergeRequest request)
    {
        if (!Sessions.TryRemove(request.Id, out var session))
        {
            return Results.NotFound("Session not found.");
        }

        if (!session.UseGit)
        {
            // For non-Git sessions, we just return the proposed changes
            return Results.Ok(new { Message = "Session closed.", ProposedChanges = session.ProposedChanges });
        }

        try
        {
            if (request.Merge)
            {
                // Commit changes using the new CommitSpecificFiles method
                var commitSuccess = await Git.CommitSpecificFiles(
                    session.GitRoot,
                    session.ProposedChanges.ChangedFiles.Select(f => f.Filename).Concat(session.ProposedChanges.NewFiles.Select(f => f.Filename)).ToList(),
                    $"Changes made by Claudia AI on branch {session.NewBranch}"
                );

                if (!commitSuccess)
                {
                    return Results.Ok("No changes to merge.");
                }

                // Switch back to the original branch
                await Git.SwitchToBranch(session.GitRoot, session.OriginalBranch);

                // Merge the new branch
                await Cli.Wrap(Git.PathToGit)
                    .WithWorkingDirectory(session.GitRoot)
                    .WithArguments($"merge {session.NewBranch}")
                    .ExecuteAsync();

                return Results.Ok("Changes merged successfully.");
            }

            await Git.SwitchToBranch(session.GitRoot, session.OriginalBranch);
            await Git.DeleteBranch(session.GitRoot, session.NewBranch);
            return Results.Ok("Changes discarded.");
        }
        catch (Exception ex)
        {
            return Results.BadRequest($"Error during merge/discard process: {ex.Message}");
        }
    }
}