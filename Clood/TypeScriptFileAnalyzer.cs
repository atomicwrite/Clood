namespace Clood;

public class TypeScriptFileAnalyzer
{
    private readonly TypeScriptSymbolTreeAnalyzer _analyzer;

    public TypeScriptFileAnalyzer()
    {
        _analyzer = new TypeScriptSymbolTreeAnalyzer();
    }

    public List<string> AnalyzeFile(string fileName)
    {
        try
        {
            // Read the file content
            string fileContent = File.ReadAllText(fileName);

            // Analyze the content
            return _analyzer.AnalyzeSymbolTree(fileContent, fileName);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Error: File not found: {fileName}");
            return new List<string>();
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error reading file {fileName}: {ex.Message}");
            return new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error analyzing file {fileName}: {ex.Message}");
            return new List<string>();
        }
    }

    public void PrintAnalysis(string fileName)
    {
        var analysis = AnalyzeFile(fileName);
        
        Console.WriteLine($"Analysis of {fileName}:");
        foreach (var item in analysis)
        {
            Console.WriteLine(item);
        }
    }
}