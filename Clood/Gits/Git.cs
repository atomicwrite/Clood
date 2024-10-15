using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using CliWrap.Exceptions;
using Serilog;

namespace Clood.Gits;

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

            if (result.ExitCode == 0) return result.StandardOutput.Trim();
            var errorMessage = $"Git command failed with exit code {result.ExitCode}. " +
                               $"Standard output: {result.StandardOutput.Trim()} " +
                               $"Standard error: {result.StandardError.Trim()}";
            Log.Error(errorMessage);
            throw new Exception($"Unable to get current branch: {errorMessage}");
 
        }
        catch (CommandExecutionException ex)
        {
            var errorMessage = $"Error executing git command: {ex.Message}. " +
                               $"Inner Exception: {ex.InnerException?.Message}. " +
                               $"Command: {ex.Command}";
            Log.Error(ex, errorMessage);
            throw new Exception($"Failed to get current branch: {errorMessage}");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Unexpected error while getting current branch: {ex.Message}";
            Log.Error(ex, errorMessage);
            throw new Exception(errorMessage);
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

        try
        {
            var result = await Cli.Wrap(PathToGit)
                .WithValidation(CommandResultValidation.None)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"checkout -b {branchName}")
                .ExecuteBufferedAsync();

            if (result.ExitCode != 0)
            {
                var errorMessage = $"Failed to create new branch. Exit code: {result.ExitCode}. " +
                                   $"Standard output: {result.StandardOutput.Trim()} " +
                                   $"Standard error: {result.StandardError.Trim()}";
                Log.Error(errorMessage);
                throw new Exception($"Could not create new branch: {errorMessage}");
            }

            Log.Information($"Created and switched to new branch: {branchName}");
            return branchName;
        }
        catch (CommandExecutionException ex)
        {
            var errorMessage = $"Error executing git command to create new branch: {ex.Message}. " +
                               $"Inner Exception: {ex.InnerException?.Message}. " +
                               $"Command: {ex.Command}";
            Log.Error(ex, errorMessage);
            throw new Exception($"Failed to create new branch: {errorMessage}");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Unexpected error while creating new branch: {ex.Message}";
            Log.Error(ex, errorMessage);
            throw new Exception(errorMessage);
        }
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
        catch (CommandExecutionException ex)
        {
            Log.Warning(ex, $"Error checking if branch {branchName} exists: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Unexpected error checking if branch {branchName} exists: {ex.Message}");
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
                    Log.Warning($"Git add failed for {file}. Exit code: {addResult.ExitCode}. " +
                                $"Standard output: {addResult.StandardOutput.Trim()} " +
                                $"Standard error: {addResult.StandardError.Trim()}");
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
                var errorMessage = $"Git commit failed. Exit code: {commitResult.ExitCode}. " +
                                   $"Standard output: {commitResult.StandardOutput.Trim()} " +
                                   $"Standard error: {commitResult.StandardError.Trim()}";
                Log.Error(errorMessage);
                return false;
            }

            Log.Information("Specific files added and committed successfully.");
            return true;
        }
        catch (CommandExecutionException ex)
        {
            var errorMessage = $"Error executing git command during commit process: {ex.Message}. " +
                               $"Inner Exception: {ex.InnerException?.Message}. " +
                               $"Command: {ex.Command}";
            Log.Error(ex, errorMessage);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Unexpected error during commit process for specific files: {ex.Message}");
            return false;
        }
    }

    private static async Task<string> GetGitStatus(string workingDirectory)
    {
        try
        {
            var result = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments("status --porcelain")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (result.ExitCode != 0)
            {
                var errorMessage = $"Git status failed. Exit code: {result.ExitCode}. " +
                                   $"Standard output: {result.StandardOutput.Trim()} " +
                                   $"Standard error: {result.StandardError.Trim()}";
                Log.Warning(errorMessage);
                return string.Empty;
            }

            return result.StandardOutput.Trim();
        }
        catch (CommandExecutionException ex)
        {
            var errorMessage = $"Error executing git status command: {ex.Message}. " +
                               $"Inner Exception: {ex.InnerException?.Message}. " +
                               $"Command: {ex.Command}";
            Log.Warning(ex, errorMessage);
            return string.Empty;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Unexpected error getting git status: {ex.Message}");
            return string.Empty;
        }
    }

    public static async Task<bool> CommitChanges(string workingDirectory, string message)
    {
        try
        {
            var statusResult = await GetGitStatus(workingDirectory);

            if (string.IsNullOrWhiteSpace(statusResult))
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
                var errorMessage = $"Git add failed. Exit code: {addResult.ExitCode}. " +
                                   $"Standard output: {addResult.StandardOutput.Trim()} " +
                                   $"Standard error: {addResult.StandardError.Trim()}";
                Log.Error(errorMessage);
                return false;
            }

            var commitResult = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"commit -m \"{message}\"")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (commitResult.ExitCode != 0)
            {
                var errorMessage = $"Git commit failed. Exit code: {commitResult.ExitCode}. " +
                                   $"Standard output: {commitResult.StandardOutput.Trim()} " +
                                   $"Standard error: {commitResult.StandardError.Trim()}";
                Log.Error(errorMessage);
                return false;
            }

            Log.Information("Changes added and committed successfully.");
            return true;
        }
        catch (CommandExecutionException ex)
        {
            var errorMessage = $"Error executing git command during commit process: {ex.Message}. " +
                               $"Inner Exception: {ex.InnerException?.Message}. " +
                               $"Command: {ex.Command}";
            Log.Error(ex, errorMessage);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Unexpected error during commit process: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> MergeChanges(string workingDirectory, string currentBranch, string newBranchName)
    {
        try
        {
            var commitSuccess =
                await CommitChanges(workingDirectory, $"Changes made by Claudia AI on branch {newBranchName}");

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
                var errorMessage = $"Merge failed. Exit code: {mergeResult.ExitCode}. " +
                                   $"Standard output: {mergeResult.StandardOutput.Trim()} " +
                                   $"Standard error: {mergeResult.StandardError.Trim()}";
                Log.Error(errorMessage);
                return false;
            }

            Log.Information($"Merged changes from {newBranchName} into {currentBranch}");
            return true;
        }
        catch (CommandExecutionException ex)
        {
            var errorMessage = $"Error executing git command during merge process: {ex.Message}. " +
                               $"Inner Exception: {ex.InnerException?.Message}. " +
                               $"Command: {ex.Command}";
            Log.Error(ex, errorMessage);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Unexpected error during merge process: {ex.Message}");
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
                var errorMessage = $"Could not switch to branch {branchName}. Exit code: {result.ExitCode}. " +
                                   $"Standard output: {result.StandardOutput.Trim()} " +
                                   $"Standard error: {result.StandardError.Trim()}";
                Log.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            Log.Information($"Switched to branch: {branchName}");
        }
        catch (CommandExecutionException ex)
        {
            var errorMessage = $"Error executing git command to switch branch: {ex.Message}. " +
                               $"Inner Exception: {ex.InnerException?.Message}. " +
                               $"Command: {ex.Command}";
            Log.Error(ex, errorMessage);
            throw new Exception($"Failed to switch to branch {branchName}: {errorMessage}");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Unexpected error while switching to branch {branchName}: {ex.Message}";
            Log.Error(ex, errorMessage);
            throw new Exception(errorMessage);
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
                var errorMessage = $"Re-checkout failed. Exit code: {checkoutResult.ExitCode}. " +
                                   $"Standard output: {checkoutResult.StandardOutput.Trim()} " +
                                   $"Standard error: {checkoutResult.StandardError.Trim()}";
                Log.Warning(errorMessage);
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
                var errorMessage = $"Git clean failed. Exit code: {cleanResult.ExitCode}. " +
                                   $"Standard output: {cleanResult.StandardOutput.Trim()} " +
                                   $"Standard error: {cleanResult.StandardError.Trim()}";
                Log.Warning(errorMessage);
            }
            else
            {
                Log.Information($"Cleaned untracked files and directories: {cleanResult.StandardOutput}");
            }
        }
        catch (CommandExecutionException ex)
        {
            var errorMessage = $"Error executing git command during RecheckoutBranchRevert: {ex.Message}. " +
                               $"Inner Exception: {ex.InnerException?.Message}. " +
                               $"Command: {ex.Command}";
            Log.Error(ex, errorMessage);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Unexpected error during RecheckoutBranchRevert: {ex.Message}");
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
                var errorMessage = $"Failed to delete branch {branchName}. Exit code: {result.ExitCode}. " +
                                   $"Standard output: {result.StandardOutput.Trim()} " +
                                   $"Standard error: {result.StandardError.Trim()}";
                Log.Warning(errorMessage);
            }
            else
            {
                Log.Information($"Deleted branch: {branchName}");
            }
        }
        catch (CommandExecutionException ex)
        {
            var errorMessage = $"Error executing git command to delete branch: {ex.Message}. " +
                               $"Inner Exception: {ex.InnerException?.Message}. " +
                               $"Command: {ex.Command}";
            Log.Error(ex, errorMessage);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Unexpected error while deleting branch {branchName}: {ex.Message}");
        }
    }

    [GeneratedRegex("[^a-zA-Z0-9-_]")]
    private static partial Regex CleanBranchRegex();
}