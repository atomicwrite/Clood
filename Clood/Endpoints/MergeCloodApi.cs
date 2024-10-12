using CliWrap;
using CliWrap.Buffered;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Clood.Endpoints;

public static class MergeCloodApi
{
    public static async Task<IResult> MergeCloodChanges([FromBody] MergeRequest request)
    {
        Log.Information("Starting MergeCloodChanges method");
        var response = new CloodResponse<string>();

        if (!CloodApiSessions.TryRemove(request.Id, out var session) || session is null)
        {
            Log.Warning("Session not found: {SessionId}", request.Id);
            response.Success = false;
            response.ErrorMessage = "Session not found.";
            return Results.Ok(response);
        }


        if (session is { UseGit: false })
        {
            Log.Information("Session {SessionId} does not use Git. No changes to apply.", request.Id);
            response.Success = true;
            response.Data = "Session closed. No changes were applied.";
            return Results.Ok(response);
        }

        try
        {
            Log.Information("Committing changes for session {SessionId}", request.Id);
            var newBranch = session.NewBranch;
            var gitRoot = session.GitRoot;
            var commitSuccess = await Git.CommitSpecificFiles(
                gitRoot,
                (session.ProposedChanges.ChangedFiles ?? []).Select(f => f.Filename)
                .Concat((session.ProposedChanges.NewFiles ?? []).Select(f => f.Filename)).ToList(),
                $"Changes made by Claudia AI on branch {newBranch}"
            );

            if (!commitSuccess)
            {
                Log.Information("No changes to merge for session {SessionId}", request.Id);
                response.Success = true;
                response.Data = "No changes to merge.";
                return Results.Ok(response);
            }

            var sessionOriginalBranch = session.OriginalBranch;
            Log.Information("Switching to original branch and merging changes for session {SessionId}", request.Id);

            await MergeCloodApiHelpers.MergeChanges(gitRoot, sessionOriginalBranch, newBranch);
            Log.Information("Successfully merged changes for session {SessionId}", request.Id);
            response.Success = true;
            response.Data = "Changes merged successfully.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process merge request for session {SessionId}", request.Id);
            response.Success = false;
            response.ErrorMessage = $"Failed to process merge request: {ex.Message}";
        }

        return Results.Ok(response);
    }
}