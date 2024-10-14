using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;

public class VueTemplateAnalyzer
{
    public List<string> AnalyzeTemplate(string template)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(template);
        var hierarchies = new HashSet<string>();

        Console.WriteLine("DOM Tree:");
        Console.WriteLine(PrintDomTree(doc));

        var rootNode = doc.DocumentNode.SelectSingleNode("//template") ?? doc.DocumentNode;
        
        if (rootNode.Name.ToLower() == "#document")
        {
            foreach (var child in rootNode.ChildNodes)
            {
                if (child.NodeType == HtmlNodeType.Element)
                {
                    AnalyzeElement(child, "", hierarchies);
                }
            }
        }
        else
        {
            AnalyzeElement(rootNode, "", hierarchies);
        }

        return hierarchies.ToList();
    }

    public string PrintDomTree(HtmlDocument doc)
    {
        var stringBuilder = new StringBuilder();
        PrintElementTree(doc.DocumentNode, "", stringBuilder);
        return stringBuilder.ToString();
    }

    private void PrintElementTree(HtmlNode node, string indent, StringBuilder stringBuilder)
    {
        if (node.NodeType == HtmlNodeType.Element)
        {
            stringBuilder.AppendLine($"{indent}{node.Name.ToLower()}");
            foreach (var attribute in node.Attributes)
            {
                stringBuilder.AppendLine($"{indent}  {attribute.Name}=\"{attribute.Value}\"");
            }
            foreach (var child in node.ChildNodes)
            {
                PrintElementTree(child, indent + "  ", stringBuilder);
            }
        }
        else if (node.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(node.InnerText))
        {
            var trimmedText = node.InnerText.Trim();
            if (!string.IsNullOrEmpty(trimmedText))
            {
                stringBuilder.AppendLine($"{indent}\"{trimmedText}\"");
            }
        }
    }

    private void AnalyzeElement(HtmlNode element, string prefix, HashSet<string> hierarchies)
    {
        var elementPrefix = string.IsNullOrEmpty(prefix) ? "/" + element.Name.ToLower() : $"{prefix}/{element.Name.ToLower()}";
        bool hasVueVariable = false;
        foreach (var attribute in element.Attributes)
        {
            if (AnalyzeAttribute(attribute, elementPrefix, hierarchies))
            {
                hasVueVariable = true;
            }
        }
        if (hasVueVariable)
        {
            hierarchies.Add(elementPrefix);
        }
        foreach (var child in element.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Element)
            {
                AnalyzeElement(child, elementPrefix, hierarchies);
            }
        }
    }

    private bool AnalyzeAttribute(HtmlAttribute attribute, string prefix, HashSet<string> hierarchies)
    {
        if (attribute.Name.StartsWith(":") || attribute.Name.StartsWith("v-bind:"))
        {
            var propName = attribute.Name.StartsWith("v-bind:") ? attribute.Name.Substring(7) : attribute.Name.Substring(1);
            hierarchies.Add($"{prefix}/:{propName}={attribute.Value}");
            return true;
        }
        else if (attribute.Name.StartsWith("@") || attribute.Name.StartsWith("v-on:"))
        {
            var methodName = attribute.Name.StartsWith("v-on:") ? attribute.Name.Substring(5) : attribute.Name.Substring(1);
            hierarchies.Add($"{prefix}/@{methodName}={attribute.Value}");
            return true;
        }
        else if (attribute.Name == "v-model")
        {
            hierarchies.Add($"{prefix}/v-model={attribute.Value}");
            return true;
        }
        else if (attribute.Name.StartsWith("v-"))
        {
            if (string.IsNullOrEmpty(attribute.Value))
            {
                hierarchies.Add($"{prefix}/+{attribute.Name}");
            }
            else
            {
                hierarchies.Add($"{prefix}/+{attribute.Name}={attribute.Value}");
            }
            return true;
        }
        return false;
    }
}