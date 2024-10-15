namespace Clood;


public class SymbolNode
{
    public string Name { get; set; }
    public SymbolKind Kind { get; set; }
    public List<SymbolNode> Children { get; set; } = new List<SymbolNode>();

    public SymbolNode(string name, SymbolKind kind)
    {
        Name = name;
        Kind = kind;
    }
}

public enum SymbolKind
{
    Class,
    Property,
    Method,
    StaticMethod,
    LocalMethod,
    Variable
}

public static class SymbolNodeExtensions
{
    public static List<string> ToTreeString(this List<SymbolNode> nodes)
    {
        var result = new List<string>();
        foreach (var node in nodes)
        {
            result.AddRange(node.ToTreeString(""));
        }
        return result;
    }

    private static List<string> ToTreeString(this SymbolNode node, string prefix)
    {
        var result = new List<string> { prefix + node.Name };
        foreach (var child in node.Children)
        {
            result.AddRange(child.ToTreeString(prefix + node.Name + ">"));
        }
        return result;
    }
}