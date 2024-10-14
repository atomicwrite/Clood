using HtmlAgilityPack;

namespace Clood;
using System;
using System.Collections.Generic;

public class VueFileAnalyzer
{
    private readonly TypeScriptSymbolTreeAnalyzer _tsAnalyzer;
    private readonly VueTemplateAnalyzer _vueAnalyzer;

    public VueFileAnalyzer()
    {
        _tsAnalyzer = new TypeScriptSymbolTreeAnalyzer();
        _vueAnalyzer = new VueTemplateAnalyzer();
    }

    public List<string> AnalyzeVueFile(string fileContent)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(fileContent);

        var templateNode = doc.DocumentNode.SelectSingleNode("//template");
        var scriptNode = doc.DocumentNode.SelectSingleNode("//script");

        var templateHierarchy = new List<string>();
        var scriptHierarchy = new List<string>();

        if (templateNode != null)
        {
            string templateContent = templateNode.InnerHtml;
            templateHierarchy = _vueAnalyzer.AnalyzeTemplate(templateContent);
        }

        if (scriptNode != null)
        {
            string scriptContent = scriptNode.InnerHtml;
            scriptHierarchy = _tsAnalyzer.AnalyzeSymbolTree(scriptContent, "script.ts");
        }

        return MergeHierarchies(templateHierarchy, scriptHierarchy);
    }

    private List<string> MergeHierarchies(List<string> templateHierarchy, List<string> scriptHierarchy)
    {
        var mergedHierarchy = new List<string>();

        // Add template hierarchy
   
        mergedHierarchy.AddRange(templateHierarchy);

  
        mergedHierarchy.AddRange(scriptHierarchy);

        return mergedHierarchy;
    }

    public void PrintHierarchy(List<string> hierarchy)
    {
        foreach (var item in hierarchy)
        {
            Console.WriteLine(item);
        }
    }
}