using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clood;

public static class RevertCloodApi
{
    public static async Task<IResult> RevertCloodChanges([FromBody] string sessionId)
    {
        var response = new CloodResponse<string>();

        if (!CloodApiSessions.TryRemove(sessionId, out var session))
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
            await Git.RecheckoutBranchRevert(session.GitRoot);
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
}