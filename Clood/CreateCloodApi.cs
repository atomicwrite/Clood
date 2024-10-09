using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clood;

public static class CreateCloodApi
{
    public static async Task<IResult> CreateCloodChanges([FromBody] CloodRequest request)
    {
        var response = new CloodResponse<CloodStartResponse>();

        var sessionId = Guid.NewGuid().ToString();
        var session = new CloodSession
        {
            UseGit = request.UseGit,
            GitRoot = CloodApi.GitRoot,
            Files = request.Files
        };

        if (request.UseGit)
        {
            var uncommittedChanges = await Clood.GetUncommittedChanges(CloodApi.GitRoot);
            if (uncommittedChanges.Count != 0)
            {
                
                response.Success = false;
                response.ErrorMessage = $"Uncommitted changes found in the repository. {string.Join("\n,",uncommittedChanges)}";
                return Results.Ok(response);
             
            }

            session.OriginalBranch = await Git.GetCurrentBranch(CloodApi.GitRoot);
            session.NewBranch = await Git.CreateNewBranch(CloodApi.GitRoot, request.Files);
        }

        var claudeResponse = await ClaudiaHelper.SendRequestToClaudia(request.Prompt, CloodApi.GitRoot,request.SystemPrompt, request.Files);

        if (string.IsNullOrWhiteSpace(claudeResponse))
        {
            if (request.UseGit)
            {
                await Git.DeleteBranch(CloodApi.GitRoot, session.NewBranch);
            }
            response.Success = false;
            response.ErrorMessage = "Invalid response from Claude AI.";
            return Results.Ok(response);
        }

        var fileChanges = ClaudiaHelper.Claudia2Json(claudeResponse);
        if (fileChanges == null)
        {
            response.Success = false;
            response.ErrorMessage = "Invalid JSON response from Claude AI.";
            return Results.Ok(response);
        }

        session.ProposedChanges = fileChanges;

        if (request.UseGit)
        {
            await Clood.ApplyChanges(fileChanges, CloodApi.GitRoot);
        }

        if (!CloodApiSessions.TryAddSession(sessionId, session))
        {
            if (request.UseGit)
            {
                await Git.DeleteBranch(CloodApi.GitRoot, session.NewBranch);
            }
            response.Success = false;
            response.ErrorMessage = "Failed to add session.";
            return Results.Ok(response);
        }

        response.Success = true;
        response.Data = new CloodStartResponse
        {
            Id = sessionId,
            NewBranch = session.NewBranch,
            ProposedChanges = session.ProposedChanges
        };

        return Results.Ok(response);
    }
}