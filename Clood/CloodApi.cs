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
        var response = new CloodResponse<string>();

        if (!Sessions.TryRemove(sessionId, out var session))
        {
            response.Success = false;
            response.ErrorMessage = "Session not found.";
            return Results.Ok(response);
        }

        if (!session.UseGit)
        {
            response.Success = true;
            response.Data = "Session removed. No changes were applied to revert.";
            return Results.Ok(response);
        }

        try
        {
            await Git.SwitchToBranch(session.GitRoot, session.OriginalBranch);
            await Git.DeleteBranch(session.GitRoot, session.NewBranch);
            response.Success = true;
            response.Data = "Changes reverted successfully. Returned to original state.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.ErrorMessage = $"Failed to revert changes: {ex.Message}";
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> StartCloodProcess([FromBody] CloodRequest request)
    {
        var response = new CloodResponse<CloodStartResponse>();

        string sessionId = Guid.NewGuid().ToString();
        CloodSession session = new CloodSession
        {
            UseGit = request.UseGit,
            GitRoot = request.GitRoot,
            Files = request.Files
        };

        if (request.UseGit)
        {
            if (await Clood.HasUncommittedChanges(request.GitRoot))
            {
                response.Success = false;
                response.ErrorMessage = "Uncommitted changes found in the repository.";
                return Results.Ok(response);
            }

            session.OriginalBranch = await Git.GetCurrentBranch(request.GitRoot);
            session.NewBranch = await Git.CreateNewBranch(request.GitRoot, request.Files);
        }

        var claudeResponse = await Clood.SendRequestToClaudia(request.Prompt, request.SystemPrompt, request.Files);

        if (string.IsNullOrWhiteSpace(claudeResponse))
        {
            if (request.UseGit)
            {
                await Git.DeleteBranch(request.GitRoot, session.NewBranch);
            }
            response.Success = false;
            response.ErrorMessage = "Invalid response from Claude AI.";
            return Results.Ok(response);
        }

        var fileChanges = ClaudiaHelper.Claudia2Json(claudeResponse);
        if (fileChanges == null)
        {
            response.Success = false;
            response.ErrorMessage = "Invalid JSON response from Claude AI.";
            return Results.Ok(response);
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
            response.Success = false;
            response.ErrorMessage = "Failed to add session.";
            return Results.Ok(response);
        }

        response.Success = true;
        response.Data = new CloodStartResponse
        {
            Id = sessionId,
            NewBranch = session.NewBranch,
            ProposedChanges = session.ProposedChanges
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> MergeCloodChanges([FromBody] MergeRequest request)
    {
        var response = new CloodResponse<string>();

        if (!Sessions.TryRemove(request.Id, out var session))
        {
            response.Success = false;
            response.ErrorMessage = "Session not found.";
            return Results.Ok(response);
        }

        if (!session.UseGit)
        {
            response.Success = true;
            response.Data = "Session closed. No changes were applied.";
            return Results.Ok(response);
        }

        try
        {
            if (request.Merge)
            {
                var commitSuccess = await Git.CommitSpecificFiles(
                    session.GitRoot,
                    session.ProposedChanges.ChangedFiles.Select(f => f.Filename).Concat(session.ProposedChanges.NewFiles.Select(f => f.Filename)).ToList(),
                    $"Changes made by Claudia AI on branch {session.NewBranch}"
                );

                if (!commitSuccess)
                {
                    response.Success = true;
                    response.Data = "No changes to merge.";
                    return Results.Ok(response);
                }

                await Git.SwitchToBranch(session.GitRoot, session.OriginalBranch);
                await Cli.Wrap(Git.PathToGit)
                    .WithWorkingDirectory(session.GitRoot)
                    .WithArguments($"merge {session.NewBranch}")
                    .ExecuteAsync();

                response.Success = true;
                response.Data = "Changes merged successfully.";
            }
            else
            {
                await Git.SwitchToBranch(session.GitRoot, session.OriginalBranch);
                await Git.DeleteBranch(session.GitRoot, session.NewBranch);
                response.Success = true;
                response.Data = "Changes discarded.";
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.ErrorMessage = $"Failed to process merge request: {ex.Message}";
        }

        return Results.Ok(response);
    }
}

public class CloodResponse<T>
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public T Data { get; set; }
}

public class CloodStartResponse
{
    public string Id { get; set; }
    public string NewBranch { get; set; }
    public FileChanges ProposedChanges { get; set; }
}