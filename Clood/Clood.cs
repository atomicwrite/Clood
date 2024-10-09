using System.Reflection;
using Claudia;
using CliWrap;
using CliWrap.Buffered;
using Markdig;
using Markdig.Syntax;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Clood;

public static class Clood
{
   

    public static async Task RunWithOptions(CliOptions opts)
    {
        if (opts.Version)
        {
            Console.WriteLine("Clood Program v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            return;
        }

        ClaudiaHelper.SetupApiKey();
        if (opts.Server)
        {
            CloodServer.Start(opts.Urls, opts.GitRoot);
            return;
        }

        await CliStart(opts);
    }

    private static async Task CliStart(CliOptions opts)
    {
        if (!string.IsNullOrEmpty(opts.GitPath))
        {
            Git.PathToGit = opts.GitPath;
        }

        Console.WriteLine("Welcome to the Claudia API and Git Integration Script!");


        var files = opts.Files.ToList();
        if (files.Count == 0)
        {
            Console.WriteLine("No files were specified.");
            return;
        }
 

        // Check for uncommitted changes
        if (await HasUncommittedChanges(opts.GitRoot))
        {
            Console.WriteLine("Warning: You have uncommitted changes in your working directory.");
            if (await GitHelpers.AskToCommitUncommited(opts.GitRoot))
                return;
        }

        var prompt = opts.Prompt;
        var systemPrompt = await GetSystemPrompt(opts);

        var response = await ClaudiaHelper.SendRequestToClaudia(prompt, opts.GitRoot, systemPrompt, files);

        if (string.IsNullOrWhiteSpace(response))
        {
            Console.WriteLine("No valid response found.");
            return;
        }

        var fileChanges = ClaudiaHelper.Claudia2Json(response);
        if (fileChanges == null)
        {
            Console.WriteLine($"No valid response from Claude AI. {response}");
            return;
        }

        var currentBranch = "";
        try
        {
            currentBranch = await Git.GetCurrentBranch(opts.GitRoot);
            var newBranchName = await Git.CreateNewBranch(opts.GitRoot, files);
            var fileContents = ClaudiaHelper.Claudia2Json(response) ??
                               throw new JsonException($"Response from claude was invalid json {response}");


            await ApplyChanges(fileChanges, opts.GitRoot);
            GitHelpers.AskToOpenFiles(files);
            await GitHelpers.AskToKeepChanges(opts.GitRoot, currentBranch, newBranchName);


            Console.WriteLine("Script execution completed.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
            await GitHelpers.AskToAbandonChanges(opts.GitRoot, currentBranch);
        }
    }

    private static async Task<string> GetSystemPrompt(CliOptions opts)
    {
        var systemPrompt = "";
        if (string.IsNullOrEmpty(opts.SystemPrompt)) return systemPrompt;
        if (File.Exists(opts.SystemPrompt))
        {
            systemPrompt = await File.ReadAllTextAsync(opts.SystemPrompt);
        }

        return systemPrompt;
    }


    public static async Task ApplyChanges(FileChanges fileChanges, string gitRoot)
    {
        gitRoot = NormalizePath(gitRoot);

        // Handle changed files
        foreach (var file in fileChanges.ChangedFiles)
        {
            var fullPath = GetFullPath(NormalizePath(file.Filename), gitRoot);
            if (fullPath == null)
            {
                Console.WriteLine($"Warning: Skipping changed file outside git root: {file.Filename}");
                continue;
            }

            await File.WriteAllTextAsync(fullPath, file.Content);
            Console.WriteLine($"Updated file: {file.Filename}");
        }

        // Handle new files
        foreach (var file in fileChanges.NewFiles)
        {
            var fullPath = GetFullPath(NormalizePath(file.Filename), gitRoot);
            if (fullPath == null)
            {
                Console.WriteLine($"Warning: Skipping new file outside git root: {file.Filename}");
                continue;
            }

            if (File.Exists(fullPath))
            {
                throw new IOException($"Error: New file already exists: {file.Filename}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            await File.WriteAllTextAsync(fullPath, file.Content);
            Console.WriteLine($"Created new file: {file.Filename}");
        }
    }

    private static string GetFullPath(string filename, string gitRoot)
    {
        var fullPath = NormalizePath(filename);

        if (Path.IsPathRooted(fullPath))
        {
            // If the path is absolute, ensure it starts with the git root
            if (!fullPath.StartsWith(gitRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null; // File is outside git root
            }
        }
        else
        {
            // If the path is relative, combine it with the git root
            fullPath = NormalizePath(Path.Combine(gitRoot, filename));

            // Ensure the resulting path is still within the git root
            if (!fullPath.StartsWith(gitRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null; // File is outside git root
            }
        }

        return fullPath;
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static async Task<List<string>> GetUncommittedChanges(string gitRoot)
    {
        try
        {
            var result = await Cli.Wrap("git")
                .WithArguments("status --porcelain")
                .WithWorkingDirectory(gitRoot)
                .ExecuteBufferedAsync();

            return result.StandardOutput
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Substring(3).Trim())
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting uncommitted changes: {ex.Message}");
            return new List<string>();
        }
    }

    public static async Task<bool> HasUncommittedChanges(string gitRoot)
    {
        try
        {
            var result = await Cli.Wrap("git")
                .WithArguments("status --porcelain")
                .WithWorkingDirectory(gitRoot)
                .ExecuteBufferedAsync();

            return !string.IsNullOrWhiteSpace(result.StandardOutput);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking for uncommitted changes: {ex.Message}");
            return false;
        }
    }
}