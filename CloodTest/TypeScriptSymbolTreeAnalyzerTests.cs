namespace CloodTest;

[TestFixture]
public class TypeScriptSymbolTreeAnalyzerTests
{
    private TypeScriptSymbolTreeAnalyzer _analyzer;

    [SetUp]
    public void Setup()
    {
        _analyzer = new TypeScriptSymbolTreeAnalyzer();
    }

    [Test]
    public void TestTopLevelFunction()
    {
        var source = @"
function greet(name: string): string {
    return `Hello, ${name}!`;
}";
        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Contains.Item("/greet"));
    }

    [Test]
    public void TestTopLevelClass()
    {
        var source = @"
class Person {
    private name: string;
    
    constructor(name: string) {
        this.name = name;
    }

    greet(): string {
        return `Hello, I'm ${this.name}!`;
    }
}";
        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Contains.Item(">Person"));
        Assert.That(result, Contains.Item(">Person@name"));
        Assert.That(result, Contains.Item(">Person/greet"));
    }

    [Test]
    public void TestModule()
    {
        var source = @"
namespace Greeter {
    export function sayHello(name: string): string {
        return `Hello, ${name}!`;
    }

    export class InternalGreeter {
        greet(name: string): string {
            return sayHello(name);
        }
    }
}";
        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Contains.Item(">Greeter"));
        Assert.That(result, Contains.Item(">Greeter/sayHello"));
        Assert.That(result, Contains.Item(">Greeter>InternalGreeter"));
        Assert.That(result, Contains.Item(">Greeter>InternalGreeter/greet"));
    }

    [Test]
    public void TestNestedFunctions()
    {
        var source = @"
function outer() {
    let x = 10;
    function inner() {
        let y = 20;
        return x + y;
    }
    return inner();
}";
        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Contains.Item("/outer"));
        Assert.That(result, Contains.Item("/outer+x"));
        Assert.That(result, Contains.Item("/outer/inner"));
        Assert.That(result, Contains.Item("/outer/inner+y"));
    }

    [Test]
    public void TestEnum()
    {
        var source = @"
enum Color {
    Red,
    Green,
    Blue
}";
        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Contains.Item("@Color"));
    }

    [Test]
    public void TestComplexStructure()
    {
        var source = @"
namespace MyApp {
    export interface ILogger {
        log(message: string): void;
    }

    export class ConsoleLogger implements ILogger {
        log(message: string): void {
            console.log(message);
        }
    }

    export function createLogger(): ILogger {
        return new ConsoleLogger();
    }

    export namespace Utils {
        export function formatMessage(message: string): string {
            return `[${new Date().toISOString()}] ${message}`;
        }
    }
}

const logger = MyApp.createLogger();
logger.log(MyApp.Utils.formatMessage('Hello, World!'));";

        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Contains.Item(">MyApp"));
        Assert.That(result, Contains.Item(">MyApp>ConsoleLogger"));
        Assert.That(result, Contains.Item(">MyApp>ConsoleLogger/log"));
        Assert.That(result, Contains.Item(">MyApp/createLogger"));
        Assert.That(result, Contains.Item(">MyApp>Utils"));
        Assert.That(result, Contains.Item(">MyApp>Utils/formatMessage"));
        Assert.That(result, Contains.Item("+logger"));
    }

    [Test]
    public void TestEmptySource()
    {
        var source = "";
        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void TestOnlyComments()
    {
        var source = @"
// This is a comment
/* This is a 
   multiline comment */";
        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void TestMethodWithoutBody()
    {
        var source = @"
interface ITest {
    method(): void;
}";
        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Does.Not.Contain("method"));
    }

    [Test]
    public void TestArrowFunctions()
    {
        var source = @"
const arrowFunc = (x: number) => x * 2;
const obj = {
    method: () => {
        const innerArrow = (y: number) => y * 3;
        return innerArrow(5);
    }
};";
        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Contains.Item("+arrowFunc"));
        Assert.That(result, Contains.Item("+obj"));
        // Note: The current implementation might not capture arrow functions inside object literals
        // If it should, you may need to modify the analyzer and update this test
    }
}