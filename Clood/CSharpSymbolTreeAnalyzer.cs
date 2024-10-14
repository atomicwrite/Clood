using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

public class CSharpSymbolTreeAnalyzer
{
    public List<string> AnalyzeSymbolTree(SyntaxNode root)
    {
        var hierarchies = new HashSet<string>();
 
        // Analyze top-level functions (including static functions)
        var topLevelFunctions = root.DescendantNodes().OfType<LocalFunctionStatementSyntax>()
            .Where(f => f.Parent is CompilationUnitSyntax or GlobalStatementSyntax);
        foreach (var function in topLevelFunctions)
        {
            hierarchies.UnionWith(AnalyzeMethodLocalFunction(function, ""));
        }

        // Analyze top-level classes
        var topLevelClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => c.Parent is CompilationUnitSyntax   or GlobalStatementSyntax);
        foreach (var classDeclaration in topLevelClasses)
        {
            hierarchies.UnionWith(AnalyzeClass(classDeclaration, ""));
        }

        return hierarchies.ToList();
    }

    private IEnumerable<string> AnalyzeClass(ClassDeclarationSyntax classDeclaration, string parentPrefix)
    {
        var hierarchies = new HashSet<string>();
        var className = classDeclaration.Identifier.Text;
        var classPrefix = string.IsNullOrEmpty(parentPrefix) ? className : $"{parentPrefix}>{className}";

        // Add the class itself to the hierarchy
        hierarchies.Add(classPrefix);

        // Analyze fields
        var fields = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Where(f => f.Parent == classDeclaration);
        foreach (var field in fields)
        {
            foreach (var variable in field.Declaration.Variables)
            {
                hierarchies.Add($"{classPrefix}>{variable.Identifier.Text}");
            }
        }

        // Analyze properties
        var properties = classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Where(p => p.Parent == classDeclaration);
        foreach (var property in properties)
        {
            hierarchies.Add($"{classPrefix}>{property.Identifier.Text}");
        }

        // Analyze methods
        var methodDeclarations = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.Parent == classDeclaration);
        foreach (var methodDeclaration in methodDeclarations)
        {
            var methodPrefix = $"{classPrefix}>{methodDeclaration.Identifier.Text}";
            
            // Add the method itself as a leaf
            hierarchies.Add(methodPrefix);

            // If it's static, add it with the "static" prefix
            if (methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                hierarchies.Add($"{classPrefix}>{methodDeclaration.Identifier.Text}");
            }

            // Analyze the method's body
            hierarchies.UnionWith(AnalyzeMethod(methodDeclaration, classPrefix));
        }

        // Analyze nested classes
        var nestedClasses = classDeclaration.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => c.Parent == classDeclaration);
        foreach (var nestedClass in nestedClasses)
        {
            hierarchies.UnionWith(AnalyzeClass(nestedClass, classPrefix));
        }

        return hierarchies;
    }
    
    private IEnumerable<string> AnalyzeMethodLocalFunction(LocalFunctionStatementSyntax methodDeclaration, string prefix)
    {
        var methodName = methodDeclaration.Identifier.Text;
        var newPrefix = string.IsNullOrEmpty(prefix) ? "" : $"{prefix}>{methodName}";
        return AnalyzeFunction(methodDeclaration, newPrefix);
    }

    private IEnumerable<string> AnalyzeMethod(MethodDeclarationSyntax methodDeclaration, string prefix)
    {
        var methodName = methodDeclaration.Identifier.Text;
        var newPrefix = $"{prefix}>{methodName}";
        return AnalyzeFunction(methodDeclaration, newPrefix);
    }

    private IEnumerable<string> AnalyzeFunction(SyntaxNode functionNode, string prefix)
    {
        var hierarchies = new HashSet<string>();
        var stack = new Stack<(SyntaxNode Node, string Prefix)>();
        stack.Push((functionNode, prefix));

        while (stack.Count > 0)
        {
            var (currentNode, currentPrefix) = stack.Pop();

            switch (currentNode)
            {
                case LocalFunctionStatementSyntax localFunction:
                    var functionName = localFunction.Identifier.Text;
                    var newPrefix = $"{currentPrefix}>{functionName}";
                    
                    // Add the local function to hierarchies
                    hierarchies.Add(newPrefix);

                    foreach (var childNode in localFunction.DescendantNodes().Reverse())
                    {
                        stack.Push((childNode, newPrefix));
                    }
                    break;

                case VariableDeclaratorSyntax variableDeclarator:
                    var variableName = variableDeclarator.Identifier.Text;
                    hierarchies.Add($"{currentPrefix}*{variableName}");
                    break;

                default:
                    foreach (var childNode in currentNode.ChildNodes().Reverse())
                    {
                        stack.Push((childNode, currentPrefix));
                    }
                    break;
            }
        }

        return hierarchies;
    }
}