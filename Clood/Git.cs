using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using Serilog;

namespace Clood;

public static partial class Git
{
    public static string PathToGit { get; set; } = "git";

    public static async Task<string> GetCurrentBranch(string workingDirectory)
    {
        try
        {
            var result = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments("rev-parse --abbrev-ref HEAD")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Could not get current branch: {result.StandardOutput} {result.StandardError}");
            }

            return result.StandardOutput.Trim();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting current branch");
            throw;
        }
    }

    public static async Task<string> CreateNewBranch(string workingDirectory, List<string> files)
    {
        var filesList = string.Join(",", files.Select(Path.GetFileName).Take(4));
        var cleanBranchRegex = CleanBranchRegex();
        var baseBranchName = cleanBranchRegex.Replace($"Clood-{filesList}", "");
        if (filesList.Length == 0)
        {
            baseBranchName = cleanBranchRegex.Replace($"Clood-empty", "");
        }

        if (baseBranchName.Length > 15)
        {
            baseBranchName = baseBranchName[..15];
        }

        var branchName = baseBranchName.Trim('-');
        var counter = 1;

        while (await BranchExists(workingDirectory, branchName))
        {
            branchName = $"{baseBranchName}-{counter}";
            counter++;
        }

        BufferedCommandResult result;
        try
        {
          result = await Cli.Wrap(PathToGit)
              .WithValidation(CommandResultValidation.None)
              .WithWorkingDirectory(workingDirectory)
              .WithArguments($"checkout -b {branchName}")
              .ExecuteBufferedAsync();
          if (result.ExitCode != 0)
          {
              throw new Exception($"Could not switch branches: {result.StandardOutput} {result.StandardError}");
          }
         
        }
        catch (Exception e)
        {
            Log.Error(e, "Error creating new branch");
            throw;
        }
        Log.Information($"Created and switched to new branch: {branchName}");
        return branchName;
    }

    private static async Task<bool> BranchExists(string workingDirectory, string branchName)
    {
        try
        {
            var result = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"rev-parse --verify {branchName}")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Error checking if branch {branchName} exists");
            return false;
        }
    }

    public static async Task<bool> CommitSpecificFiles(string workingDirectory, List<string> files, string message)
    {
        try
        {
            foreach (var file in files)
            {
                var addResult = await Cli.Wrap(PathToGit)
                    .WithWorkingDirectory(workingDirectory)
                    .WithArguments($"add \"{file}\"")
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync();

                if (addResult.ExitCode != 0)
                {
                    Log.Warning($"Git add failed for {file}: {addResult.StandardError}");
                }
                else
                {
                    Log.Information($"Git add successful for {file}");
                }
            }

            var afterStatus = await GetGitStatus(workingDirectory);
            Log.Information($"Status after adding specific files: {afterStatus}");

            if (string.IsNullOrWhiteSpace(afterStatus))
            {
                Log.Information("No changes to commit after adding specific files.");
                return false;
            }

            var commitResult = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"commit -m \"{message}\"")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (commitResult.ExitCode != 0)
            {
                Log.Error($"Git commit failed: {commitResult.StandardError}");
                return false;
            }

            Log.Information("Specific files added and committed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during commit process for specific files");
            return false;
        }
    }

    private static async Task<string> GetGitStatus(string workingDirectory)
    {
        var result = await Cli.Wrap(PathToGit)
            .WithWorkingDirectory(workingDirectory)
            .WithArguments("status --porcelain")
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        if (result.ExitCode != 0)
        {
            Log.Warning($"Git status failed: {result.StandardError}");
            return string.Empty;
        }

        return result.StandardOutput.Trim();
    }

    public static async Task<bool> CommitChanges(string workingDirectory, string message)
    {
        try
        {
            var statusResult = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments("status --porcelain")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (statusResult.ExitCode != 0)
            {
                Log.Warning($"Git status failed: {statusResult.StandardError}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(statusResult.StandardOutput))
            {
                Log.Information("No changes to commit.");
                return false;
            }

            var addResult = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments("add .")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (addResult.ExitCode != 0)
            {
                Log.Error($"Git add failed: {addResult.StandardError}");
                return false;
            }

            var commitResult = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"commit -m \"{message}\"")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (commitResult.ExitCode != 0)
            {
                Log.Error($"Git commit failed: {commitResult.StandardError}");
                return false;
            }

            Log.Information("Changes added and committed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during commit process");
            return false;
        }
    }

    public static async Task<bool> MergeChanges(string workingDirectory, string currentBranch, string newBranchName)
    {
        try
        {
            var commitSuccess = await CommitChanges(workingDirectory, $"Changes made by Claudia AI on branch {newBranchName}");

            if (!commitSuccess)
            {
                Log.Information("No changes to merge.");
                return false;
            }

            await SwitchToBranch(workingDirectory, currentBranch);

            var mergeResult = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"merge {newBranchName}")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (mergeResult.ExitCode != 0)
            {
                Log.Error($"Merge failed: {mergeResult.StandardError}");
                return false;
            }

            Log.Information($"Merged changes from {newBranchName} into {currentBranch}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during merge process");
            return false;
        }
    }

    public static async Task SwitchToBranch(string workingDirectory, string branchName)
    {
        try
        {
            var result = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"checkout {branchName}")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Could not switch to branch {branchName}: {result.StandardError}");
            }

            Log.Information($"Switched to branch: {branchName}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error switching to branch {branchName}");
            throw;
        }
    }

    public static async Task RecheckoutBranchRevert(string workingDirectory)
    {
        try
        {
            var checkoutResult = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments("checkout .")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (checkoutResult.ExitCode != 0)
            {
                Log.Warning($"Re-checkout failed: {checkoutResult.StandardError}");
            }
            else
            {
                Log.Information($"Re-checked out from {workingDirectory}");
            }

            var cleanResult = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments("clean -fd")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (cleanResult.ExitCode != 0)
            {
                Log.Warning($"Git clean failed: {cleanResult.StandardError}");
            }
            else
            {
                Log.Information($"Cleaned untracked files and directories: {cleanResult.StandardOutput}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during RecheckoutBranchRevert");
        }
    }

    public static async Task DeleteBranch(string workingDirectory, string branchName)
    {
        try
        {
            var result = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"branch -D {branchName}")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (result.ExitCode != 0)
            {
                Log.Warning($"Failed to delete branch {branchName}: {result.StandardError}");
            }
            else
            {
                Log.Information($"Deleted branch: {branchName}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error deleting branch {branchName}");
        }
    }

    [GeneratedRegex("[^a-zA-Z0-9-_]")]
    private static partial Regex CleanBranchRegex();
}