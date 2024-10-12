using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Clood.Endpoints;

public static class DiscardCloodApi
{
    public static async Task<IResult> DiscardCloodChanges( DiscardRequest request,
        CloodSession session)
    {
        Log.Information("Starting MergeCloodChanges method");
        var response = new CloodResponse<string>();

        if (session is not { UseGit: false })
            return await MergeCloodApiHelpers.DiscardChagnes(request, session, response);
        Log.Information("Session {SessionId} does not use Git. No changes to apply.", request.Id);
        response.Success = true;
        response.Data = "Session closed. No changes were applied.";
        return Results.Ok(response);
    }
}