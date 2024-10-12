using CliWrap;
using CliWrap.Buffered;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Clood.Endpoints;

public static class MergeCloodApiHelpers
{
    public static async Task MergeChanges(string gitRoot, string sessionOriginalBranch, string newBranch)
    {
        await Git.SwitchToBranch(gitRoot, sessionOriginalBranch);
        var result = await Cli.Wrap(Git.PathToGit)
            .WithWorkingDirectory(gitRoot)
            .WithValidation(CommandResultValidation.None)
            .WithArguments($"merge {newBranch}")
            .ExecuteBufferedAsync();

        if (result.ExitCode != 0)
        {
            throw new Exception($"Could not merge {result.StandardOutput} {result.StandardError}");
        }
    }

   
}