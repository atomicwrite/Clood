using Microsoft.AspNetCore.Http;
using Serilog;

namespace Clood;

public static class CloodApiErrorHandlers
{
    public static IResult CloodMergeRevertResponse(Exception e)
    {
        Log.Error(e, "CloodMergeRevertResponse error");
        return Results.Ok(new CloodResponse<string>()
        {
            Data = "",
            ErrorMessage = e.Message,
            Success = false
        });
    }

    public static IResult CloodPromptErrorResponse(Exception e)
    {
        Log.Error(e, "CloodPromptErrorResponse error");
        return Results.Ok(new CloodResponse<string>()
        {
            Data = "",
            ErrorMessage = e.Message,
            Success = false
        });
    }

    public static IResult CloodDiscardErrorResponse(Exception e)
    {
        Log.Error(e, "CloodMergeErrorResponse error");
        return Results.Ok(new CloodResponse<string>()
        {
            Data = "",
            ErrorMessage = e.Message,
            Success = false
        });
    }

    public static IResult CloodMergeErrorResponse(Exception e)
    {
        Log.Error(e, "CloodMergeErrorResponse error");
        return Results.Ok(new CloodResponse<string>()
        {
            Data = "",
            ErrorMessage = e.Message,
            Success = false
        });
    }

    public static IResult CloodStartErrorResponse(Exception e)
    {
        Log.Error(e, "CloodStartErrorResponse error");
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