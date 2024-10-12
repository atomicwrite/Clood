using CliWrap;
using CliWrap.Buffered;
using Clood.Files;

namespace Clood.Helpers;

public static class CreateCloodHelper
{
    public static async Task ApplyChanges(FileChanges fileChanges, string gitRoot)
    {
        gitRoot = NormalizePath(gitRoot);


        foreach (var file in fileChanges.ChangedFiles!)
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
        foreach (var file in fileChanges.NewFiles!)
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

            Directory.CreateDirectory(Path.GetDirectoryName((string?)fullPath) ??
                                      throw new InvalidOperationException(
                                          $"Could not get Directory name of {fullPath}"));
            await File.WriteAllTextAsync(fullPath, file.Content);
            Console.WriteLine($"Created new file: {file.Filename}");
        }
    }

    private static string? GetFullPath(string filename, string gitRoot)
    {
        var fullPath = NormalizePath(filename);

        if (Path.IsPathRooted((string?)fullPath))
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