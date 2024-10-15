using Clood.Endpoints.API;
using Clood.Endpoints.DTO;
using Clood.Errors;
using Clood.Files;
using Clood.Gits;
using Clood.Helpers;
using Clood.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Clood.Endpoints;

public static class CreateCloodApi
{
    public static async Task<IResult> CreateCloodChanges([FromBody] CloodRequest request)
    {
        Log.Information("Starting CreateCloodChanges method");
        var response = new CloodResponse<CloodStartResponse>();

        var files = await CheckAndGetCreateCloodFiles(request.Files, request.UseGit);

        var session = CloodApiSessions.CreateSession(request.UseGit, request.Files);

        if (request.UseGit)
        {
            await SwapToNewBranch(request.Files, session);
        }

        session.ProposedChanges = await GetClaudeFileChanges(request.Prompt, files);


        Log.Information("Received proposed changes from Claude AI");

        if (request.UseGit)
        {
            Log.Information("Applying changes to Git repository");
            await CreateCloodHelper.ApplyChanges(session.ProposedChanges, CloodApi.GitRoot);
        }

        await AddNewSession(request.UseGit, session);

        Log.Information("Successfully created Clood session {SessionId}", session.Id);
        response.Success = true;
        response.Data = new CloodStartResponse
        {
            Id = session.Id,
            NewBranch = session.NewBranch,
            ProposedChanges = session.ProposedChanges,
        };

        return Results.Ok(response);
    }

    private static async Task AddNewSession(bool useGit, CloodSession session)
    {
        if (CloodApiSessions.TryAddSession(session.Id, session))
        {
            return;
        }

        Log.Error("Failed to add session {SessionId}", session.Id);
        if (useGit)
        {
            await Git.DeleteBranch(CloodApi.GitRoot, session.NewBranch);
            Log.Information("Deleted branch {NewBranch} due to session creation failure", session.NewBranch);
        }

        throw new SessionException("Failed to add session.");
    }

    private static async Task SwapToNewBranch(List<string> fileList, CloodSession session)
    {
        session.OriginalBranch = await Git.GetCurrentBranch(CloodApi.GitRoot);
        Log.Debug("Original branch: {OriginalBranch}", session.OriginalBranch);
        session.NewBranch = await Git.CreateNewBranch(CloodApi.GitRoot, fileList);
        Log.Information("Created new branch: {NewBranch}", session.NewBranch);
    }

    private static async Task<List<string>> CheckAndGetCreateCloodFiles(List<string> files, bool useGit)
    {
        var missing = files.Where(a => !File.Exists(Path.Join(CloodApi.GitRoot, a))).ToArray();

        if (missing.Length != 0)
        {
            Log.Warning("Files are missing: {MissingFiles}", string.Join(",", missing));
            throw new MissingFilesException($"Files are missing in this folder {string.Join(",", missing)}");
        }


        if (useGit)
        {
            Log.Information("Git is enabled for this session");
            var uncommittedChanges = await CreateCloodHelper.GetUncommittedChanges(CloodApi.GitRoot);
            if (uncommittedChanges.Count != 0)
            {
                Log.Warning("Uncommitted changes found in the repository: {UncommittedChanges}",
                    string.Join("\n,", uncommittedChanges));
                throw new UncommittedFilesException(
                    $"Uncommitted changes found in the repository. {string.Join("\n,", uncommittedChanges)}");
            }
        }

        return files.Select(a => Path.Join(CloodApi.GitRoot, a)).ToList();
    }

    private static async Task<FileChanges> GetClaudeFileChanges(string prompt, List<string> files)
    {
        Log.Information("Sending request to Claude AI");

        var claudeResponse =
            await ClaudiaHelper.SendRequestToClaudia(prompt, CloodApi.GitRoot, files);


        var fileChanges = ClaudiaHelper.Claudia2Json(claudeResponse);
        if (fileChanges == null)
        {
            throw new ClaudeCouldNotAnswerException("Invalid JSON response from Claude AI.");
        }


        if (fileChanges.Answered)
        {
            fileChanges.ChangedFiles ??= [];
            fileChanges.NewFiles ??= [];
            return fileChanges;
        }

        Log.Warning("Claude AI could not answer the question");
        throw new ClaudeCouldNotAnswerException("Claude could not answer the question.");
    }
}