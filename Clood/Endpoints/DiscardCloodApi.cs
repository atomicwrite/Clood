using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Clood.Endpoints;

public static class DiscardCloodApi
{
    public static async Task<IResult> DiscardCloodChanges(DiscardRequest request,
        CloodSession session)
    {
        Log.Information("Starting MergeCloodChanges method");
        var response = new CloodResponse<string>();
        
        Log.Information("Discarding changes for session {SessionId}", request.Id);
        
        await Git.SwitchToBranch(session.GitRoot, session.OriginalBranch);
        await Git.DeleteBranch(session.GitRoot, session.NewBranch);
        await Git.RecheckoutBranchRevert(session.GitRoot);
        response.Success = true;
        response.Data = "Changes discarded.";
        return Results.Ok(response);
    }
}