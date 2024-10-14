using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Clood;

public class CSharpSymbolTreeAnalyzer
{
    public List<SymbolNode> AnalyzeFile(string filePath)
    {
        var code = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        var symbols = new List<SymbolNode>();
        foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var classNode = new SymbolNode(classDeclaration.Identifier.Text, SymbolKind.Class);
            symbols.Add(classNode);
            AnalyzeClassMembers(classDeclaration, classNode);
        }

        return symbols;
    }

    private void AnalyzeClassMembers(ClassDeclarationSyntax classDeclaration, SymbolNode classNode)
    {
        foreach (var member in classDeclaration.Members)
        {
            switch (member)
            {
                case PropertyDeclarationSyntax property:
                    classNode.Children.Add(new SymbolNode(property.Identifier.Text, SymbolKind.Property));
                    break;
                case MethodDeclarationSyntax method:
                    var methodKind = method.Modifiers.Any(SyntaxKind.StaticKeyword) ? SymbolKind.StaticMethod : SymbolKind.Method;
                    var methodNode = new SymbolNode(method.Identifier.Text, methodKind);
                    classNode.Children.Add(methodNode);
                    AnalyzeMethodBody(method, methodNode);
                    break;
                // Add more cases for other member types as needed
            }
        }
    }

    private void AnalyzeMethodBody(MethodDeclarationSyntax method, SymbolNode methodNode)
    {
        if (method.Body == null) return;

        foreach (var node in method.Body.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclarationSyntax variable:
                    foreach (var declarator in variable.Variables)
                    {
                        var parentScope = FindParentScope(declarator);
                        if (parentScope == method)
                        {
                            methodNode.Children.Add(new SymbolNode(declarator.Identifier.Text, SymbolKind.Variable));
                        }
                    }
                    break;
                case LocalFunctionStatementSyntax localFunction:
                    var localFunctionNode = new SymbolNode(localFunction.Identifier.Text, SymbolKind.LocalMethod);
                    methodNode.Children.Add(localFunctionNode);
                    AnalyzeLocalFunctionBody(localFunction, localFunctionNode);
                    break;
            }
        }
    }

    private void AnalyzeLocalFunctionBody(LocalFunctionStatementSyntax localFunction, SymbolNode localFunctionNode)
    {
        if (localFunction.Body == null) return;

        foreach (var node in localFunction.Body.DescendantNodes())
        {
            if (node is VariableDeclarationSyntax variable)
            {
                foreach (var declarator in variable.Variables)
                {
                    var parentScope = FindParentScope(declarator);
                    if (parentScope == localFunction)
                    {
                        localFunctionNode.Children.Add(new SymbolNode(declarator.Identifier.Text, SymbolKind.Variable));
                    }
                }
            }
        }
    }

    private static SyntaxNode FindParentScope(VariableDeclaratorSyntax declarator)
    {
        var parent = declarator.Parent;
        while (parent != null)
        {
            if (parent is MethodDeclarationSyntax || parent is LocalFunctionStatementSyntax)
            {
                return parent;
            }
            parent = parent.Parent;
        }
        return null;
    }
}