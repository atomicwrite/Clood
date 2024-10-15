using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class CSharpSymbolTreeAnalyzerTests
{
    private CSharpSymbolTreeAnalyzer _analyzer;

    [SetUp]
    public void Setup()
    {
        _analyzer = new CSharpSymbolTreeAnalyzer();
    }

    [Test]
    public void TestTopLevelFunction()
    {
        var sourceCode = @"
static void TopLevelFunction()
{
    int x = 5;
}";

        var result = AnalyzeCode(sourceCode);

        Assert.That(result, Has.Exactly(2).Items);
        Assert.That(result, Does.Contain("/TopLevelFunction+x"));
    }

    [Test]
    public void TestNestedFunctions()
    {
        var sourceCode = @"
static void OuterFunction()
{
    int x = 1;
    static void InnerFunction()
    {
        int y = 2;
    }
}";

        var result = AnalyzeCode(sourceCode);

        Assert.That(result, Has.Exactly(5).Items);
        Assert.That(result, Does.Contain("/OuterFunction+x"));
        Assert.That(result, Does.Contain("/OuterFunction/InnerFunction+y"));
    }

    [Test]
    public void TestNestedFunctions2()
    {
        var sourceCode = @"
static void OuterFunction()
{
    int x = 1;
    static void InnerFunction()
    {
        int y = 2;
    }
}
class MyClass
{
    void Method1()
    {
        int a = 1;
    }

    void Method2()
    {
        int b = 2;
        void LocalFunction()
        {
 int c = 3;
        }
    }



";

        var result = AnalyzeCode(sourceCode);

        Assert.That(result, Has.Exactly(12).Items);
        Assert.That(result, Does.Contain("/OuterFunction+x"));
        Assert.That(result, Does.Contain("/OuterFunction/InnerFunction+y"));
    }

    [Test]
    public void TestClassWithMethods()
    {
        var sourceCode = @"
class MyClass
{
    void Method1()
    {
        int a = 1;
    }

    void Method2()
    {
        int b = 2;
        void LocalFunction()
        {
 int c = 3;
        }
    }
}";

        var result = AnalyzeCode(sourceCode);

        Assert.That(result, Has.Exactly(7).Items);
        Assert.That(result, Does.Contain(">MyClass/Method1+a"));
        Assert.That(result, Does.Contain(">MyClass/Method2+b"));
        Assert.That(result, Does.Contain(">MyClass/Method2/LocalFunction+c"));
    }

    [Test]
    public void TestFullFile()
    {
        var sourceCode = File.ReadAllText("CloodApi.cs.txt");

        var result = AnalyzeCode(sourceCode);
 

        var expectedHierarchies = new List<string>
        {
            ">CloodApi",
            ">CloodApi@GitRoot",
            ">CloodApi/ConfigureApi",
            ">CloodApi/ConfigureApi+result",
            ">CloodApi/ConfigureApi+response",
        };

        Assert.That(result, Has.Exactly(expectedHierarchies.Count).Items);
        Assert.That(result, Is.EquivalentTo(expectedHierarchies));
    }

    [Test]
    public void TestComplexClassStructure()
    {
        var sourceCode = @"
public class OuterClass
{
    public string OuterProperty { get; set; }

    public void OuterMethod()
    {
        var outerVar = 42;

        void LocalMethod1()
        {
 var localMethod1Var = ""test"";

 void NestedLocalMethod()
 {
     var nestedVar = true;
 }
        }

        var anotherOuterVar = 10;

        void LocalMethod2()
        {
 var localMethod2Var = 3.14;
        }
    }

    public static void StaticMethod()
    {
        var staticMethodVar = 100;

        void StaticLocalMethod()
        {
 var staticLocalVar = ""static local"";
        }
    }

    private class InnerClass
    {
        public int InnerProperty { get; set; }

        public void InnerMethod()
        {
 var innerVar = 1000;

 void InnerLocalMethod()
 {
     var innerLocalVar = ""inner local"";
 }
        }
    }
}";

        var result = AnalyzeCode(sourceCode);

        var expectedHierarchies = new List<string>
        {
            ">OuterClass",
            ">OuterClass@OuterProperty",
            ">OuterClass/OuterMethod",
            ">OuterClass/OuterMethod+outerVar",
            ">OuterClass/OuterMethod/LocalMethod1",
            ">OuterClass/OuterMethod/LocalMethod1+localMethod1Var",
            ">OuterClass/OuterMethod/LocalMethod1/NestedLocalMethod",
            ">OuterClass/OuterMethod/LocalMethod1/NestedLocalMethod+nestedVar",
            ">OuterClass/OuterMethod/LocalMethod1+nestedVar",
            ">OuterClass/OuterMethod+anotherOuterVar",
            ">OuterClass/OuterMethod/LocalMethod2",
            ">OuterClass/OuterMethod/LocalMethod2+localMethod2Var",
            ">OuterClass/StaticMethod",
            ">OuterClass/StaticMethod+staticMethodVar",
            ">OuterClass/StaticMethod/StaticLocalMethod",
            ">OuterClass/StaticMethod/StaticLocalMethod+staticLocalVar",
            ">OuterClass>InnerClass",
            ">OuterClass>InnerClass@InnerProperty",
            ">OuterClass>InnerClass/InnerMethod",
            ">OuterClass>InnerClass/InnerMethod+innerVar",
            ">OuterClass>InnerClass/InnerMethod/InnerLocalMethod",
            ">OuterClass>InnerClass/InnerMethod/InnerLocalMethod+innerLocalVar"
        };

        Assert.That(result, Has.Exactly(expectedHierarchies.Count).Items);
        Assert.That(result, Is.EquivalentTo(expectedHierarchies));
    }

    private List<string> AnalyzeCode(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();
        return _analyzer.AnalyzeSymbolTree(root);
    }
}