using Clood.Endpoints.DTO;

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
                    var symbols = analyzer.AnalyzeFile(file);
                    var treeStrings = symbols.ToTreeString();
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