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
            Console.WriteLine($"Clood Program v{Assembly.GetExecutingAssembly().GetName().Version}");
            return;
        }


        if (opts.Server)
        {
            CloodServer.Start(opts.Urls, opts.GitRoot);
            return;
        }
 
    }
 

 


    public static async Task ApplyChanges(FileChanges fileChanges, string gitRoot)
    {
        gitRoot = NormalizePath(gitRoot);


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

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ??
                                      throw new InvalidOperationException(
                                          $"Could not get Directory name of {fullPath}"));
            await File.WriteAllTextAsync(fullPath, file.Content);
            Console.WriteLine($"Created new file: {file.Filename}");
        }
    }

    private static string? GetFullPath(string filename, string gitRoot)
    {
        var fullPath = NormalizePath(filename);

        if (Path.IsPathRooted(fullPath))
        {
            
            if (!fullPath.StartsWith(gitRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null;  
            }

            return fullPath;
        }

        
        fullPath = NormalizePath(Path.Combine(gitRoot, filename));

          
        if (!fullPath.StartsWith(gitRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;  
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