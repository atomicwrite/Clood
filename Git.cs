﻿using CliWrap;
using CliWrap.Buffered;

namespace Clood;

public static class Git
{
    public static string PathToGit { get; set; } = "git";
    public static async Task<string> GetCurrentBranch(string workingDirectory)
    {
        var result = await Cli.Wrap(PathToGit)
            .WithWorkingDirectory(workingDirectory)
            .WithArguments("rev-parse --abbrev-ref HEAD")
            .ExecuteBufferedAsync();
        return result.StandardOutput.Trim();
    }

    public static async Task<string> CreateNewBranch(string workingDirectory, List<string> files)
    {
        var filesList = string.Join(",", files.Select(Path.GetFileName).Take(4));
        var baseBranchName = $"Modifications-{filesList}";
    
        // Remove invalid characters and truncate if necessary
        baseBranchName = new string(baseBranchName.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
        if (baseBranchName.Length > 50)
        {
            baseBranchName = baseBranchName.Substring(0, 47) + "...";
        }

        var branchName = baseBranchName;
        var counter = 1;

        while (await BranchExists(workingDirectory, branchName))
        {
            branchName = $"{baseBranchName}-{counter}";
            counter++;
        }

        
        await Cli.Wrap(PathToGit)
            .WithWorkingDirectory(workingDirectory)
            .WithArguments($"checkout -b {branchName}")
            .ExecuteAsync();

        Console.WriteLine($"Created and switched to new branch: {branchName}");
        return branchName;
    }

    private static async Task<bool> BranchExists(string workingDirectory, string branchName)
    {
        try
        {
            var result = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"rev-parse --verify {branchName}")
                .ExecuteBufferedAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task CommitChanges(string workingDirectory, string message)
    {
        // First, add all changes
        await Cli.Wrap(PathToGit)
            .WithWorkingDirectory(workingDirectory)
            .WithArguments("add *")
            .ExecuteAsync();

        // Then, commit the changes
        await Cli.Wrap(PathToGit)
            .WithWorkingDirectory(workingDirectory)
            .WithArguments($"commit -m \"{message}\"")
            .ExecuteAsync();

        Console.WriteLine("Changes added and committed successfully.");
    }

    public static async Task MergeChanges(string workingDirectory, string currentBranch, string newBranchName)
    {
        // First, commit changes on the new branch
        await CommitChanges(workingDirectory, $"Changes made by Claudia AI on branch {newBranchName}");

        // Switch back to the original branch
        await SwitchToBranch(workingDirectory, currentBranch);

        // Merge the new branch
        await Cli.Wrap(PathToGit)
            .WithWorkingDirectory(workingDirectory)
            .WithArguments($"merge {newBranchName}")
            .ExecuteAsync();

        Console.WriteLine($"Merged changes from {newBranchName} into {currentBranch}");
    }

    public static async Task SwitchToBranch(string workingDirectory, string branchName)
    {
        await Cli.Wrap(PathToGit)
            .WithWorkingDirectory(workingDirectory)
            .WithArguments($"checkout {branchName}")
            .ExecuteAsync();
        Console.WriteLine($"Switched to branch: {branchName}");
    }

    public static async Task DeleteBranch(string workingDirectory, string branchName)
    {
        await Cli.Wrap(PathToGit)
            .WithWorkingDirectory(workingDirectory)
            .WithArguments($"branch -D {branchName}")
            .ExecuteAsync();
        Console.WriteLine($"Deleted branch: {branchName}");
    }
}