using Clood.Endpoints.DTO;
using Clood.Files;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Clood.Errors;

public static class CloodApiErrorHandlers
{
    
    public static IResult CloodAnalyzeFilesErrorResponse(Exception e)
    {
        Log.Error(e, "CloodAnalyzeFilesErrorResponse error");
        return Results.Ok(new CloodResponse<List<string>>()
        {
            Data = new List<string>(),
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