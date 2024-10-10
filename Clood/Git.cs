using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;

namespace Clood;

public static partial class Git
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
          result =  await Cli.Wrap(PathToGit)
              .WithValidation(CommandResultValidation.None)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"checkout -b {branchName}")
                .ExecuteBufferedAsync();
          Console.WriteLine($"Git git branch: {result.StandardOutput}");
          Console.WriteLine($"Git git branch: {result.StandardError}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
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

    public static async Task<bool> CommitSpecificFiles(string workingDirectory, List<string> files, string message)
    {
        try
        {
            // Add each file individually
            foreach (var file in files)
            {
                var addResult = await Cli.Wrap(PathToGit)
                    .WithWorkingDirectory(workingDirectory)
                    .WithArguments($"add \"{file}\"")
                    .ExecuteBufferedAsync();

                Console.WriteLine($"Git add output for {file}: {addResult.StandardOutput}");
                Console.WriteLine($"Git add error for {file} (if any): {addResult.StandardError}");
            }

            // Check for changes after adding
            var afterStatus = await GetGitStatus(workingDirectory);
            Console.WriteLine($"Status after adding specific files: {afterStatus}");

            if (string.IsNullOrWhiteSpace(afterStatus))
            {
                Console.WriteLine("No changes to commit after adding specific files.");
                return false;
            }

            // Commit the changes
            var commitResult = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"commit -m \"{message}\"")
                .ExecuteBufferedAsync();

            Console.WriteLine($"Git commit output: {commitResult.StandardOutput}");
            Console.WriteLine($"Git commit error (if any): {commitResult.StandardError}");

            Console.WriteLine("Specific files added and committed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during commit process for specific files: {ex.Message}");
            return false;
        }
    }

    private static async Task<string> GetGitStatus(string workingDirectory)
    {
        var result = await Cli.Wrap(PathToGit)
            .WithWorkingDirectory(workingDirectory)
            .WithArguments("status --porcelain")
            .ExecuteBufferedAsync();

        return result.StandardOutput.Trim();
    }

    public static async Task<bool> CommitChanges(string workingDirectory, string message)
    {
        try
        {
            // First, check if there are any changes to commit
            var statusResult = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments("status --porcelain")
                .ExecuteBufferedAsync();

            if (string.IsNullOrWhiteSpace(statusResult.StandardOutput))
            {
                Console.WriteLine("No changes to commit.");
                return false;
            }

            // Add all changes
            await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments("add .")
                .ExecuteAsync();

            // Commit the changes
            await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"commit -m \"{message}\"")
                .ExecuteAsync();

            Console.WriteLine("Changes added and committed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during commit process: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> MergeChanges(string workingDirectory, string currentBranch, string newBranchName)
    {
        try
        {
            // First, commit changes on the new branch
            var commitSuccess =
                await CommitChanges(workingDirectory, $"Changes made by Claudia AI on branch {newBranchName}");

            if (!commitSuccess)
            {
                Console.WriteLine("No changes to merge.");
                return false;
            }

            // Switch back to the original branch
            await SwitchToBranch(workingDirectory, currentBranch);

            // Merge the new branch
            await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments($"merge {newBranchName}")
                .ExecuteAsync();

            Console.WriteLine($"Merged changes from {newBranchName} into {currentBranch}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during merge process: {ex.Message}");
            return false;
        }
    }

    public static async Task SwitchToBranch(string workingDirectory, string branchName)
    {
        await Cli.Wrap(PathToGit)
            .WithWorkingDirectory(workingDirectory)
            .WithArguments($"checkout {branchName}")
            .ExecuteAsync();
        Console.WriteLine($"Switched to branch: {branchName}");
    }

    public static async Task RecheckoutBranchRevert(string workingDirectory)
    {
        try
        {
            await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments("checkout .")
                .ExecuteBufferedAsync();
            Console.WriteLine($"Re-checked out from {workingDirectory}");

            // Run git clean -fd
            var cleanResult = await Cli.Wrap(PathToGit)
                .WithWorkingDirectory(workingDirectory)
                .WithArguments("clean -fd")
                .ExecuteBufferedAsync();
            Console.WriteLine($"Cleaned untracked files and directories: {cleanResult.StandardOutput}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during RecheckoutBranchRevert: {ex.Message}");
        }
    }

    public static async Task DeleteBranch(string workingDirectory, string branchName)
    {
        await Cli.Wrap(PathToGit)
            .WithWorkingDirectory(workingDirectory)
            .WithArguments($"branch -D {branchName}")
            .ExecuteAsync();
        Console.WriteLine($"Deleted branch: {branchName}");
    }

    [GeneratedRegex("[^a-zA-Z0-9-_]")]
    private static partial Regex CleanBranchRegex();
}