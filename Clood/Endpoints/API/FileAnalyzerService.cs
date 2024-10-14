using Clood.Endpoints.DTO;
using Microsoft.CodeAnalysis.CSharp;

namespace Clood.Endpoints.API;

public static class FileAnalyzerService
{
    public static List<string> AnalyzeFiles(AnalyzeFilesRequest request)
    {
        var result = new List<string>();
        var analyzer = new CSharpSymbolTreeAnalyzer();

        foreach (var file in request.Files)
        {
            if (!file.StartsWith(CloodApi.GitRoot))
            {
                throw new InvalidFilePathException($"The file is not in the Git root: {file}");
            }

            var extension = Path.GetExtension(file);

            switch (extension)
            {
                case ".cs":
                {
                    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
                    var root = tree.GetRoot();
                    var treeStrings = analyzer.AnalyzeSymbolTree(root);
                    result.AddRange(treeStrings.Select(s => $"{Path.GetFileName(file)}:{s}"));
                    break;
                }
                default:

                    break;
            }
        }

        return result;
    }
}