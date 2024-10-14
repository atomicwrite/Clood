using System.Text.RegularExpressions;
using Clood.Endpoints.DTO;
using Microsoft.CodeAnalysis.CSharp;

namespace Clood.Endpoints.API;

public static partial class FileAnalyzerService
{
    private static readonly Regex DirSplitter = MyRegex();
    public static List<string> AnalyzeFiles(AnalyzeFilesRequest request)
    {
        var result = new List<string>();
        var analyzer = new CSharpSymbolTreeAnalyzer();

        foreach (var file in request.Files)
        {
            string fullPath;
            try
            {
                
                var segments = DirSplitter.Split(file);
                if (segments.Any((fileName) => fileName.StartsWith("..") || fileName.Contains("//") ||
                                               fileName.Contains("\\\\") ||
                                               fileName.StartsWith(@"/") || fileName.StartsWith(@"\")))

                {
                    throw new InvalidFileNameException($"Suspicious file path detected: {file}");
                }

                // If it's a relative path, append CloodApi.GitRoot
                if (!Path.IsPathRooted(file))
                {
                    fullPath = Path.GetFullPath(Path.Combine(CloodApi.GitRoot, file));
                }
                else
                {
                    fullPath = Path.GetFullPath(file); // Normalize the path
                }

                // Ensure the full path is within the GitRoot
                if (!fullPath.StartsWith(CloodApi.GitRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidFilePathException($"The file is not in the Git root: {fullPath}");
                }

                // Additional check to ensure the normalized path doesn't escape GitRoot
                var relativePath = Path.GetRelativePath(CloodApi.GitRoot, fullPath);
                if (relativePath.StartsWith("..") || Path.IsPathRooted(relativePath))
                {
                    throw new InvalidFilePathException($"Invalid file path: {file}");
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is PathTooLongException ||
                                       ex is NotSupportedException)
            {
                throw new InvalidFilePathException($"Invalid file path: {file}");
            }

            var extension = Path.GetExtension(fullPath);
            switch (extension.ToLowerInvariant())
            {
                case ".cs":
                {
                    if (!File.Exists(fullPath))
                    {
                        throw new FileNotFoundException($"File not found: {fullPath}");
                    }

                    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(fullPath));
                    var root = tree.GetRoot();
                    var treeStrings = analyzer.AnalyzeSymbolTree(root);
                    result.AddRange(treeStrings.Select(s => $"{Path.GetFileName(fullPath)}:{s}"));
                    break;
                }
                default:
                    // Optionally, you might want to log unsupported file types
                    // logger.LogWarning($"Unsupported file type: {extension} for file {fullPath}");
                    break;
            }
        }

        return result;
    }

    [GeneratedRegex("[\\/]")]
    private static partial Regex MyRegex();
}

public class InvalidFileNameException(string msg) : Exception(msg)
{
}