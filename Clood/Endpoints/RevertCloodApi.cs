using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Clood;

public static class RevertCloodApi
{
    public static async Task<IResult> RevertCloodChanges([FromBody] string sessionId)
    {
        Log.Information("Starting RevertCloodChanges method for session {SessionId}", sessionId);
        var response = new CloodResponse<string>();

        if (!CloodApiSessions.TryRemove(sessionId, out var session))
        {
            Log.Warning("Session not found: {SessionId}", sessionId);
            response.Success = false;
            response.ErrorMessage = "Session not found.";
            return Results.Ok(response);
        }

        if (!session.UseGit)
        {
            Log.Information("Session {SessionId} does not use Git. No changes to revert.", sessionId);
            response.Success = true;
            response.Data = "Session removed. No changes were applied to revert.";
            return Results.Ok(response);
        }

        try
        {
            Log.Information("Reverting changes for session {SessionId}", sessionId);
            await Git.SwitchToBranch(session.GitRoot, session.OriginalBranch);
            await Git.DeleteBranch(session.GitRoot, session.NewBranch);
            await Git.RecheckoutBranchRevert(session.GitRoot);
            Log.Information("Successfully reverted changes for session {SessionId}", sessionId);
            response.Success = true;
            response.Data = "Changes reverted successfully. Returned to original state.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to revert changes for session {SessionId}", sessionId);
            response.Success = false;
            response.ErrorMessage = $"Failed to revert changes: {ex.Message}";
        }

        return Results.Ok(response);
    }
}