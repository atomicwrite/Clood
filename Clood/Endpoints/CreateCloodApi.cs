using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Clood;

public static class CreateCloodApi
{
    public static async Task<IResult> CreateCloodChanges([FromBody] CloodRequest request)
    {
        Log.Information("Starting CreateCloodChanges method");
        var response = new CloodResponse<CloodStartResponse>();
        
        var missing = request.Files.Where(a => !File.Exists(Path.Join(CloodApi.GitRoot, a))).ToArray();
        if (missing.Length != 0)
        {
            Log.Warning("Files are missing: {MissingFiles}", string.Join(",", missing));
            response.Success = false;
            response.ErrorMessage = $"Files are missing in this folder {string.Join(",", missing)}";
            return Results.Ok(response);
        }

        var sessionId = Guid.NewGuid().ToString();
        Log.Debug("Generated new session ID: {SessionId}", sessionId);
        var session = new CloodSession
        {
            UseGit = request.UseGit,
            GitRoot = CloodApi.GitRoot,
            Files = request.Files
        };

        if (request.UseGit)
        {
            Log.Information("Git is enabled for this session");
            var uncommittedChanges = await Clood.GetUncommittedChanges(CloodApi.GitRoot);
            if (uncommittedChanges.Count != 0)
            {
                Log.Warning("Uncommitted changes found in the repository: {UncommittedChanges}", string.Join("\n,", uncommittedChanges));
                response.Success = false;
                response.ErrorMessage =
                    $"Uncommitted changes found in the repository. {string.Join("\n,", uncommittedChanges)}";
                return Results.Ok(response);
            }

            session.OriginalBranch = await Git.GetCurrentBranch(CloodApi.GitRoot);
            Log.Debug("Original branch: {OriginalBranch}", session.OriginalBranch);
            try
            {
                session.NewBranch = await Git.CreateNewBranch(CloodApi.GitRoot, request.Files);
                Log.Information("Created new branch: {NewBranch}", session.NewBranch);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to create new branch");
                response.Success = false;
                response.ErrorMessage =
                    $"Could not create new Branch {e.Message}";
                return Results.Ok(response);
            }
        }

        Log.Information("Sending request to Claude AI");
        var claudeResponse =
            await ClaudiaHelper.SendRequestToClaudia(request.Prompt, CloodApi.GitRoot, request.SystemPrompt,
                request.Files.Select(a=>Path.Join(CloodApi.GitRoot,a)).ToList());

        if (string.IsNullOrWhiteSpace(claudeResponse))
        {
            Log.Warning("Received invalid response from Claude AI");
            if (request.UseGit)
            {
                await Git.DeleteBranch(CloodApi.GitRoot, session.NewBranch);
                Log.Information("Deleted branch {NewBranch} due to invalid Claude AI response", session.NewBranch);
            }

            response.Success = false;
            response.ErrorMessage = "Invalid response from Claude AI.";
            return Results.Ok(response);
        }

        var fileChanges = ClaudiaHelper.Claudia2Json(claudeResponse);
        if (fileChanges == null)
        {
            Log.Warning("Failed to parse Claude AI response as JSON");
            response.Success = false;
            response.ErrorMessage = "Invalid JSON response from Claude AI.";
            return Results.Ok(response);
        }

        if (!fileChanges.Answered)
        {
            Log.Warning("Claude AI could not answer the question");
            response.Success = false;
            response.ErrorMessage = "Claude could not answer the question.";
            return Results.Ok(response);
        }

        session.ProposedChanges = fileChanges;
        Log.Information("Received proposed changes from Claude AI");

        if (request.UseGit)
        {
            Log.Information("Applying changes to Git repository");
            await Clood.ApplyChanges(fileChanges, CloodApi.GitRoot);
        }

        if (!CloodApiSessions.TryAddSession(sessionId, session))
        {
            Log.Error("Failed to add session {SessionId}", sessionId);
            if (request.UseGit)
            {
                await Git.DeleteBranch(CloodApi.GitRoot, session.NewBranch);
                Log.Information("Deleted branch {NewBranch} due to session creation failure", session.NewBranch);
            }

            response.Success = false;
            response.ErrorMessage = "Failed to add session.";
            return Results.Ok(response);
        }

        Log.Information("Successfully created Clood session {SessionId}", sessionId);
        response.Success = true;
        response.Data = new CloodStartResponse
        {
            Id = sessionId,
            NewBranch = session.NewBranch,
            ProposedChanges = session.ProposedChanges,
        };

        return Results.Ok(response);
    }
}