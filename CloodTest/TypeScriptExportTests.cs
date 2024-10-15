namespace CloodTest;

[TestFixture]
public class TypeScriptExportTests
{
    private TypeScriptSymbolTreeAnalyzer _analyzer;

    [SetUp]
    public void Setup()
    {
        _analyzer = new TypeScriptSymbolTreeAnalyzer();
    }

    [Test]
    public void TestDefaultExport()
    {
        var code = @"
export default {
    name: 'MyComponent',
    setup() {
        return { prop1: 'value1' }
    }
}";
        var result = _analyzer.AnalyzeSymbolTree(code, "test.ts");
        Assert.That(result, Does.Contain(">default"));
        Assert.That(result, Does.Contain(">default@name"));
        Assert.That(result, Does.Contain(">default/setup"));
    }

    [Test]
    public void TestNamedExports()
    {
        var code = @"
export const namedConst = 'value';
export function namedFunction() {}
export class NamedClass {}";
        var result = _analyzer.AnalyzeSymbolTree(code, "test.ts");
        Assert.That(result, Does.Contain("+namedConst"));
        Assert.That(result, Does.Contain("/namedFunction"));
        Assert.That(result, Does.Contain(">NamedClass"));
    }

    [Test]
    public void TestExportConst()
    {
        var code = @"
export const obj = {
    prop1: 'value1',
    method1() {}
}";
        var result = _analyzer.AnalyzeSymbolTree(code, "test.ts");
        Assert.That(result, Does.Contain("+obj"));
        Assert.That(result, Does.Contain("+obj@prop1"));
        Assert.That(result, Does.Contain("+obj/method1"));
    }

    

    [Test]
    public void TestMixedExports()
    {
        var code = @"
export const namedConst = 'value';
export default class DefaultClass {
    method() {}
}
export function namedFunction() {}";
        var result = _analyzer.AnalyzeSymbolTree(code, "test.ts");
        Assert.That(result, Does.Contain("+namedConst"));
        Assert.That(result, Does.Contain(">DefaultClass"));
        Assert.That(result, Does.Contain(">DefaultClass/method"));
        Assert.That(result, Does.Contain("/namedFunction"));
    }
}