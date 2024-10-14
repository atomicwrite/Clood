using System;
using System.Collections.Generic;
using System.Linq;
using Zu.TypeScript;
using Zu.TypeScript.TsTypes;

public class TypeScriptSymbolTreeAnalyzer
{
    
    private void AnalyzeEnum(EnumDeclaration enumDeclaration, string parentPrefix, HashSet<string> hierarchies)
    {
        var enumName = enumDeclaration.IdentifierStr;
        var enumPrefix = string.IsNullOrEmpty(parentPrefix) ? "@" + enumName : $"{parentPrefix}@{enumName}";
        hierarchies.Add(enumPrefix);

        foreach (var member in enumDeclaration.Members)
        {
            var memberName = member.Name.GetText();
            hierarchies.Add($"{enumPrefix}>{memberName}");
        }
    }
    public List<string> AnalyzeSymbolTree(string sourceCode, string fileName)
    {
        var ast = new TypeScriptAST(sourceCode, fileName);
        var hierarchies = new HashSet<string>();

        AnalyzeNode(ast.RootNode, "", hierarchies);

        return hierarchies.ToList();
    }

    private void AnalyzeNode(Node node, string prefix, HashSet<string> hierarchies)
    {
        switch (node)
        {
            case SourceFile sourceFile:
                foreach (var child in sourceFile.Children)
                {
                    AnalyzeNode(child, prefix, hierarchies);
                }
                break;
            case ModuleDeclaration module:
                AnalyzeModule(module, prefix, hierarchies);
                break;
            case ClassDeclaration classDecl:
                AnalyzeClass(classDecl, prefix, hierarchies);
                break;
            case FunctionDeclaration function:
                AnalyzeFunction(function, prefix, hierarchies);
                break;
            case VariableStatement variableStatement:
                AnalyzeVariableStatement(variableStatement, prefix, hierarchies);
                break;
            case EnumDeclaration enumDecl:
                AnalyzeEnum(enumDecl, prefix, hierarchies);
                break;
        }
    }

    private void AnalyzeModule(ModuleDeclaration module, string parentPrefix, HashSet<string> hierarchies)
    {
        var moduleName = module.IdentifierStr;
        var modulePrefix = string.IsNullOrEmpty(parentPrefix) ? ">" + moduleName : $"{parentPrefix}>{moduleName}";
        hierarchies.Add(modulePrefix);

        foreach (var child in module.Body.Children)
        {
            AnalyzeNode(child, modulePrefix, hierarchies);
        }
    }

    private void AnalyzeClass(ClassDeclaration classDeclaration, string parentPrefix, HashSet<string> hierarchies)
    {
        var className = classDeclaration.IdentifierStr;
        var classPrefix = string.IsNullOrEmpty(parentPrefix) ? ">" + className : $"{parentPrefix}>{className}";
        hierarchies.Add(classPrefix);

        foreach (var member in classDeclaration.Members)
        {
            switch (member)
            {
                case PropertyDeclaration property:
                    hierarchies.Add($"{classPrefix}@{property.IdentifierStr}");
                    break;
                case MethodDeclaration method:
                    AnalyzeMethod(method, classPrefix, hierarchies);
                    break;
            }
        }
    }

    private void AnalyzeMethod(MethodDeclaration method, string prefix, HashSet<string> hierarchies)
    {
        var methodName = method.IdentifierStr;
        var methodPrefix = $"{prefix}/{methodName}";
        hierarchies.Add(methodPrefix);

        if (method.Body != null)
        {
            foreach (var child in method.Body.Children)
            {
                AnalyzeNode(child, methodPrefix, hierarchies);
            }
        }
    }

    private void AnalyzeFunction(FunctionDeclaration function, string prefix, HashSet<string> hierarchies)
    {
        var functionName = function.IdentifierStr;
        var functionPrefix = $"{prefix}/{functionName}";
        hierarchies.Add(functionPrefix);

        if (function.Body != null)
        {
            foreach (var child in function.Body.Children)
            {
                AnalyzeNode(child, functionPrefix, hierarchies);
            }
        }
    }

    private void AnalyzeVariableStatement(VariableStatement variableStatement, string prefix, HashSet<string> hierarchies)
    {
        foreach (var declaration in variableStatement.DeclarationList.Declarations)
        {
            var varName = declaration.IdentifierStr;
            var varPrefix = string.IsNullOrEmpty(prefix) ? "+" + varName : $"{prefix}+{varName}";
            hierarchies.Add(varPrefix);

            if (declaration.Initializer is ArrowFunction arrowFunction)
            {
                AnalyzeArrowFunction(arrowFunction, varPrefix, hierarchies);
            }
            else if (declaration.Initializer is ObjectLiteralExpression objectLiteral)
            {
                AnalyzeObjectLiteral(objectLiteral, varPrefix, hierarchies);
            }
        }
    }

    private void AnalyzeArrowFunction(ArrowFunction arrowFunction, string prefix, HashSet<string> hierarchies)
    {
        if (arrowFunction.Body is Block block)
        {
            foreach (var child in block.Children)
            {
                AnalyzeNode(child, prefix, hierarchies);
            }
        }
    }

    private void AnalyzeObjectLiteral(ObjectLiteralExpression objectLiteral, string prefix, HashSet<string> hierarchies)
    {
        foreach (var property in objectLiteral.Properties)
        {
            if (property is PropertyAssignment propAssignment)
            {
                var propName = propAssignment.PropertyName?.GetText() ?? propAssignment.Name.GetText();
                var propPrefix = $"{prefix}@{propName}";
                hierarchies.Add(propPrefix);

                if (propAssignment.Initializer is ArrowFunction arrowFunction)
                {
                    AnalyzeArrowFunction(arrowFunction, propPrefix, hierarchies);
                }
            }
        }
    }
}