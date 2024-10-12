using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Clood.Endpoints;

public static class RevertCloodApi
{
    public static async Task<IResult> RevertCloodChanges(CloodSession session)
    {
  
        Log.Information("Starting RevertCloodChanges method for session { session.Id}",  session.Id);
        var response = new CloodResponse<string>();
 

        if (!session.UseGit)
        {
            Log.Information("Session { session.Id} does not use Git. No changes to revert.",  session.Id);
            response.Success = true;
            response.Data = "Session removed. No changes were applied to revert.";
            return Results.Ok(response);
        }

        try
        {
            Log.Information("Reverting changes for session { session.Id}",  session.Id);
            await Git.SwitchToBranch(session.GitRoot, session.OriginalBranch);
            await Git.DeleteBranch(session.GitRoot, session.NewBranch);
            await Git.RecheckoutBranchRevert(session.GitRoot);
            Log.Information("Successfully reverted changes for session { session.Id}",  session.Id);
            response.Success = true;
            response.Data = "Changes reverted successfully. Returned to original state.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to revert changes for session { session.Id}",  session.Id);
            response.Success = false;
            response.ErrorMessage = $"Failed to revert changes: {ex.Message}";
        }

        return Results.Ok(response);
    }
}