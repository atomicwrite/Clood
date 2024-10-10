using CliWrap;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clood;

public static class MergeCloodApi
{
    public static async Task<IResult> MergeCloodChanges([FromBody] MergeRequest request)
    {
        var response = new CloodResponse<string>();

        if (!CloodApiSessions.TryRemove(request.Id, out var session))
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
            if (!request.Merge)
            {
                await Git.SwitchToBranch(session.GitRoot, session.OriginalBranch);
                await Git.DeleteBranch(session.GitRoot, session.NewBranch);
                await Git.RecheckoutBranchRevert(session.GitRoot);
                response.Success = true;
                response.Data = "Changes discarded.";
                return Results.Ok(response);
            }

            var commitSuccess = await Git.CommitSpecificFiles(
                session.GitRoot,
                (session.ProposedChanges.ChangedFiles ?? []).Select(f => f.Filename)
                    .Concat((session.ProposedChanges.NewFiles ?? []).Select(f => f.Filename)).ToList(),
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
        catch (Exception ex)
        {
            response.Success = false;
            response.ErrorMessage = $"Failed to process merge request: {ex.Message}";
        }

        return Results.Ok(response);
    }
}