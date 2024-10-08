using System.Diagnostics;

namespace Clood;

public static class GitHelpers
{
    public static async Task<bool> AskToCommitUncommited(string workingDirectory)
    {
        Console.Write("Do you want to commit these changes before proceeding? (y/n): ");
        if (Console.ReadLine().ToLower() == "y")
        {
            Console.Write("Enter a commit message: ");
            var commitMessage = Console.ReadLine();
            await Git.CommitChanges(workingDirectory, commitMessage);
            Console.WriteLine("Changes committed successfully.");
        }
        else
        {
            Console.Write(
                "Proceeding with uncommitted changes might affect the script's behavior. Do you want to exit? (y/n): ");
            if (Console.ReadLine().ToLower() == "y")
            {
                Console.WriteLine("Exiting script. No changes were made.");
                return true;
            }

            Console.WriteLine("Proceeding with uncommitted changes. Please be cautious.");
        }

        return false;
    }

    public static async Task AskToAbandonChanges(string workingDirectory, string currentBranch)
    {
        Console.Write("Do you want to abandon changes? (y/n): ");
        if (Console.ReadLine().ToLower() == "y")
        {
            await Git.SwitchToBranch(workingDirectory, currentBranch);
            Console.WriteLine("Changes abandoned. Switched back to the original branch.");
        }
        else
        {
            Console.WriteLine("Attempting to merge changes despite the error...");
            try
            {
                var currentBranchName = await Git.GetCurrentBranch(workingDirectory);
                await Git.CommitChanges(workingDirectory, "Changes made by Claudia AI (with errors)");
                await Git.MergeChanges(workingDirectory, currentBranch, currentBranchName);
                Console.WriteLine("Changes merged successfully.");
            }
            catch (Exception mergeEx)
            {
                Console.WriteLine($"Error merging changes: {mergeEx.Message}");
                Console.WriteLine("You may need to resolve conflicts manually.");
            }
        }
    }

    public static void AskToOpenFiles(List<string> files)
    {
        Console.Write("Do you want to open the modified files in your current editor? (y/n): ");
        if (Console.ReadLine().ToLower() != "y") return;
        foreach (var file in files)
        {
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
        }
    }

    public static async Task AskToKeepChanges(string workingDirectory, string currentBranch, string newBranchName)
    {
        Console.Write("Do you want to keep the changes? (y/n): ");
        var keep = Console.ReadLine().ToLower() == "y";
        if (keep)
        {
            await Git.CommitChanges(workingDirectory, "Changes made by Claudia AI");
            await Git.MergeChanges(workingDirectory, currentBranch, newBranchName);
            return;
        }

        await Git.SwitchToBranch(workingDirectory, currentBranch);
        await Git.DeleteBranch(workingDirectory, newBranchName);
    }
}