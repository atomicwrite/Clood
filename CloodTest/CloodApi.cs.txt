using Clood.Endpoints.DTO;
using Clood.Errors;
using Clood.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Clood.Endpoints.API;

public static class CloodApi
{
    public static string GitRoot { get; set; } = null!;

    public static void ConfigureApi(WebApplication app, string gitRoot)
    {
        
        GitRoot = gitRoot;
        Log.Information("API configured with GitRoot: {GitRoot}", GitRoot);
        app.MapPost("/api/clood/analyze-files", async ([FromBody] AnalyzeFilesRequest request) =>
        {
            try
            {
                Log.Information("Starting file analysis for request: {@Request}", request);

                var result = FileAnalyzerService.AnalyzeFiles(request);

                return Results.Ok(new CloodResponse<List<string>>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (InvalidFilePathException e)
            {
                Log.Error(e, "Invalid file path error occurred while analyzing files");
                return CloodApiErrorHandlers.CloodAnalyzeFilesErrorResponse(e);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occurred while analyzing files");
                return CloodApiErrorHandlers.CloodAnalyzeFilesErrorResponse(e);
            }
        });

   
    
        app.MapPost("/api/clood/prompt", async ([FromBody] CloodPromptRequest request) =>
        {
            try
            {
                Log.Information("Starting Clood changes for request: {@Request}", request);
                return await CreateCloodPrompt.CreateCloodPromptChanges(request);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occurred while starting Clood changes");
                return CloodApiErrorHandlers.CloodPromptErrorResponse(e);
            }
        });
        app.MapPost("/api/clood/start", async ([FromBody] CloodRequest request) =>
        {
            try
            {
                Log.Information("Starting Clood changes for request: {@Request}", request);
                return await CreateCloodApi.CreateCloodChanges(request);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occurred while starting Clood changes");
                return CloodApiErrorHandlers.CloodStartErrorResponse(e);
            }
        });


        app.MapPost("/api/clood/discard",
            async ([FromBody] DiscardRequest request ) =>
            {
                try
                {
                    if (!CloodApiSessions.TryRemove(request.Id, out var session) || session == null)
                    {
                        throw new SessionException("Could not find Session");
                    }
                    Log.Information("Merging Clood changes for request: {@Request}", request);
                    if (session is not { UseGit: false })
                        return await DiscardCloodApi.DiscardCloodChanges(request, session);

                    var response = new CloodResponse<string>();
                    Log.Information("Session {SessionId} does not use Git. No changes to apply.", request.Id);
                    response.Success = true;
                    response.Data = "Session closed. No changes were applied.";
                    return Results.Ok(response);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error occurred while discarding Clood changes");
                    return CloodApiErrorHandlers.CloodDiscardErrorResponse(e);
                }
            });
        app.MapPost("/api/clood/merge", async ([FromBody] MergeRequest request) =>
        {
            try
            {
                if (!CloodApiSessions.TryRemove(request.Id, out var session) || session == null)
                {
                    throw new SessionException("Could not find Session");
                }
                Log.Information("Merging Clood changes for request: {@Request}", request);
                return await MergeCloodApi.MergeCloodChanges(request, session);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occurred while merging Clood changes");
                return CloodApiErrorHandlers.CloodMergeErrorResponse(e);
            }
        });
    

        GitRoot = gitRoot;
        Log.Information("API configured with GitRoot: {GitRoot}", GitRoot);
    }
}