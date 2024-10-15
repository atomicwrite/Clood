using System;
using System.Collections.Generic;
using System.Linq;
using Zu.TypeScript;
using Zu.TypeScript.TsTypes;

public class TypeScriptSymbolTreeAnalyzer
{
    public List<string> AnalyzeSymbolTree(string sourceCode, string fileName)
    {
        var ast = new TypeScriptAST(sourceCode, fileName);
        var hierarchies = new HashSet<string>();
        Console.WriteLine(ast.GetTreeString());
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
            case InterfaceDeclaration interfaceDecl:
                AnalyzeInterface(interfaceDecl, prefix, hierarchies);
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
            case ExportAssignment exportAssignment:
                AnalyzeExportAssignment(exportAssignment, prefix, hierarchies);
                break;
        }
    }

    private void AnalyzeInterface(InterfaceDeclaration interfaceDecl, string prefix, HashSet<string> hierarchies)
    {
        var interfaceName = interfaceDecl.IdentifierStr;
        var interfacePrefix = string.IsNullOrEmpty(prefix) ? ">" + interfaceName : $"{prefix}>{interfaceName}";
        hierarchies.Add(interfacePrefix);

        foreach (var member in interfaceDecl.Members)
        {
            switch (member)
            {
                case PropertySignature property:
                    hierarchies.Add($"{interfacePrefix}@{property.IdentifierStr}");
                    break;
                case MethodSignature method:
                    hierarchies.Add($"{interfacePrefix}/{method.IdentifierStr}");
                    break;
            }
        }
    }

    private void AnalyzeExportAssignment(ExportAssignment exportAssignment, string prefix, HashSet<string> hierarchies)
    {
        string exportPrefix;

        if (exportAssignment.IsExportEquals)
        {
            // This is a default export
            exportPrefix = string.IsNullOrEmpty(prefix) ? ">default" : $"{prefix}>default";
            hierarchies.Add(exportPrefix);
        }
        else
        {
            // This is a named export or 'export const'
            exportPrefix = prefix;
        }

        switch (exportAssignment.Expression)
        {
            case ObjectLiteralExpression objectLiteral:
                if (string.IsNullOrEmpty(exportPrefix))
                {
            
                    exportPrefix = ">default";
                    hierarchies.Add(exportPrefix);
                }

                AnalyzeObjectLiteral(objectLiteral, exportPrefix, hierarchies);
                break;
            case Identifier identifier:
                var identifierName = identifier.GetText();
                hierarchies.Add($"{exportPrefix}>{identifierName}");
                break;
            case CallExpression callExpression:
                AnalyzeCallExpression(callExpression, exportPrefix, hierarchies);
                break;
            case VariableDeclaration variableDeclaration:
                // This handles 'export const' cases
                AnalyzeVariableDeclaration(variableDeclaration, exportPrefix, hierarchies);
                break;
        }
    }

    private void AnalyzeVariableDeclaration(VariableDeclaration variableDeclaration, string prefix,
        HashSet<string> hierarchies)
    {
        var varName = variableDeclaration.IdentifierStr;
        var varPrefix = string.IsNullOrEmpty(prefix) ? "+" + varName : $"{prefix}+{varName}";
        hierarchies.Add(varPrefix);

        if (variableDeclaration.Initializer is ObjectLiteralExpression objectLiteral)
        {
            AnalyzeObjectLiteral(objectLiteral, varPrefix, hierarchies);
        }
        else if (variableDeclaration.Initializer != null)
        {
            AnalyzeExpression(variableDeclaration.Initializer, varPrefix, hierarchies);
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

    private void AnalyzeFunction(FunctionExpression function, string prefix, HashSet<string> hierarchies)
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

    private void AnalyzeVariableStatement(VariableStatement variableStatement, string prefix,
        HashSet<string> hierarchies)
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

    

    private void AnalyzeObjectLiteral(ObjectLiteralExpression objectLiteral, string prefix, HashSet<string> hierarchies)
    {
        foreach (var property in objectLiteral.Properties)
        {
            switch (property)
            {
                case PropertyAssignment propAssignment:
                    var propName = propAssignment.PropertyName?.GetText() ?? propAssignment.Name.GetText();
                    var propPrefix = string.IsNullOrEmpty(prefix) ? propName : $"{prefix}@{propName}";
                    hierarchies.Add(propPrefix);

                    AnalyzePropertyInitializer(propAssignment.Initializer, propPrefix, hierarchies);
                    break;

                case MethodDeclaration methodDecl:
                    var methodName = methodDecl.IdentifierStr;
                    var methodPrefix = string.IsNullOrEmpty(prefix) ? methodName : $"{prefix}/{methodName}";
                    hierarchies.Add(methodPrefix);

                    AnalyzeMethodBody(methodDecl.Body, methodPrefix, hierarchies);
                    break;

                case ShorthandPropertyAssignment shorthandProp:
                    var shorthandName = shorthandProp.Name.GetText();
                    var shorthandPrefix = string.IsNullOrEmpty(prefix) ? shorthandName : $"{prefix}+{shorthandName}";
                    hierarchies.Add(shorthandPrefix);
                    break;
            }
        }
    }

    private void AnalyzePropertyInitializer(IExpression initializer, string prefix, HashSet<string> hierarchies)
    {
        switch (initializer)
        {
            case ArrowFunction arrowFunction:
                AnalyzeArrowFunction(arrowFunction, prefix, hierarchies);
                break;
            case FunctionExpression functionExpression:
                AnalyzeFunction(functionExpression, prefix, hierarchies);
                break;
            case ObjectLiteralExpression nestedObjectLiteral:
                AnalyzeObjectLiteral(nestedObjectLiteral, prefix, hierarchies);
                break;
            // Add more cases as needed for other types of initializers
        }
    }

    private void AnalyzeMethodBody(IBlockOrExpression body, string prefix, HashSet<string> hierarchies)
    {
        switch (body)
        {
            case Block blockBody:
            {
                foreach (var statement in blockBody.Statements)
                {
                    AnalyzeStatement(statement, prefix, hierarchies);
                }

                break;
            }
            case IExpression expression:
                AnalyzeExpression(expression, prefix, hierarchies);
                break;
        }
    }

    private void AnalyzeArrowFunction(ArrowFunction arrowFunction, string prefix, HashSet<string> hierarchies)
    {
        if (arrowFunction.Body is Block block)
        {
            foreach (var statement in block.Statements)
            {
                AnalyzeStatement(statement, prefix, hierarchies);
            }
        }
        else if (arrowFunction.Body is Expression expression)
        {
            AnalyzeExpression(expression, prefix, hierarchies);
        }
    }

    private void AnalyzeStatement(IStatement statement, string prefix, HashSet<string> hierarchies)
    {
        switch (statement)
        {
            case VariableStatement variableStatement:
                AnalyzeVariableStatement(variableStatement, prefix, hierarchies);
                break;
            case ExpressionStatement expressionStatement:
                AnalyzeExpression(expressionStatement.Expression, prefix, hierarchies);
                break;
            case ReturnStatement returnStatement:
                if (returnStatement.Expression != null)
                {
                    AnalyzeExpression(returnStatement.Expression, prefix, hierarchies);
                }

                break;
            // Add more cases for other types of statements as needed
        }
    }

    private void AnalyzeExpression(IExpression expression, string prefix, HashSet<string> hierarchies)
    {
        switch (expression)
        {
            case CallExpression callExpression:
                AnalyzeCallExpression(callExpression, prefix, hierarchies);
                break;
            case ObjectLiteralExpression objectLiteral:
                AnalyzeObjectLiteral(objectLiteral, prefix, hierarchies);
                break;
            case ArrowFunction nestedArrowFunction:
                AnalyzeArrowFunction(nestedArrowFunction, prefix, hierarchies);
                break;
            case BinaryExpression binaryExpression:
                AnalyzeBinaryExpression(binaryExpression, prefix, hierarchies);
                break;
            // Add more cases as needed for other expression types
        }
    }

    private void AnalyzeCallExpression(CallExpression callExpression, string prefix, HashSet<string> hierarchies)
    {
        var functionName = callExpression.Expression.GetText();
        hierarchies.Add($"{prefix}/{functionName}");

        foreach (var argument in callExpression.Arguments)
        {
            AnalyzeExpression(argument, $"{prefix}/{functionName}", hierarchies);
        }
    }

    private void AnalyzeBinaryExpression(BinaryExpression binaryExpression, string prefix, HashSet<string> hierarchies)
    {
        AnalyzeExpression(binaryExpression.Left, prefix, hierarchies);
        AnalyzeExpression(binaryExpression.Right, prefix, hierarchies);
    }
}