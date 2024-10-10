using System.Collections.Concurrent;
using CliWrap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clood;

public static class CloodApi
{
    public static string GitRoot { get; set; } = null!;

    public static void ConfigureApi(WebApplication app, string gitRoot)
    {
        app.MapPost("/api/clood/start", async ([FromBody] CloodRequest request) =>
        {
            try
            {
                return await CreateCloodApi.CreateCloodChanges(request);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return CloodStartErrorResponse(e);
            }
        });
        app.MapPost("/api/clood/merge", async ([FromBody] MergeRequest request) =>
        {
            try
            {
                return await MergeCloodApi.MergeCloodChanges(request);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return CloodMergeErrorResponse(e);
            }
        });
        app.MapPost("/api/clood/revert", async ([FromBody] string request) =>
        {
            try
            {
                return await RevertCloodApi.RevertCloodChanges(request);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return CloodMergeRevertResponse(e);
            }
        });
 
        GitRoot = gitRoot;
    }

    private static IResult CloodMergeRevertResponse(Exception e)
    {
        return Results.Ok(new CloodResponse<string>()
        {
            Data = "",
            ErrorMessage = e.Message,
            Success = false
        });
    }

    private static IResult CloodMergeErrorResponse(Exception e)
    {
        return Results.Ok(new CloodResponse<string>()
        {
            Data = "",
            ErrorMessage = e.Message,
            Success = false
        });
    }

    private static IResult CloodStartErrorResponse(Exception e)
    {
        return Results.Ok(new CloodResponse<CloodStartResponse>()
        {
            Data = new CloodStartResponse()
            {
                Id = "-1",
                NewBranch = "",
                ProposedChanges = new FileChanges()
                {
                    Answered = false,
                    ChangedFiles = [],
                    NewFiles = [],
                }
            },
            ErrorMessage = e.Message,
            Success = false
        });
    }
}