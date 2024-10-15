namespace CloodTest;

[TestFixture]
public class TypeScriptSymbolTreeAnalyzerTests2
{
    private TypeScriptSymbolTreeAnalyzer _analyzer;

    [SetUp]
    public void Setup()
    {
        _analyzer = new TypeScriptSymbolTreeAnalyzer();
    }

   
    [Test]
    public void TestInterface()
    {
        var source = @"
interface User {
    name: string;
    age: number;
    greet(): void;
}";
        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Contains.Item(">User"));
        Assert.That(result, Contains.Item(">User@name"));
        Assert.That(result, Contains.Item(">User@age"));
        Assert.That(result, Contains.Item(">User/greet"));
    }

    [Test]
    public void TestExportedInterface()
    {
        var source = @"
export interface ILogger {
    log(message: string): void;
}";
        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Contains.Item(">ILogger"));
        Assert.That(result, Contains.Item(">ILogger/log"));
    }

    [Test]
    public void TestComplexStructureWithInterfaces()
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
        export interface IFormatter {
            format(message: string): string;
        }

        export function formatMessage(message: string): string {
            return `[${new Date().toISOString()}] ${message}`;
        }
    }
}

const logger = MyApp.createLogger();
logger.log(MyApp.Utils.formatMessage('Hello, World!'));";

        var result = _analyzer.AnalyzeSymbolTree(source, "test.ts");
        Assert.That(result, Contains.Item(">MyApp"));
        Assert.That(result, Contains.Item(">MyApp>ILogger"));
        Assert.That(result, Contains.Item(">MyApp>ILogger/log"));
        Assert.That(result, Contains.Item(">MyApp>ConsoleLogger"));
        Assert.That(result, Contains.Item(">MyApp>ConsoleLogger/log"));
        Assert.That(result, Contains.Item(">MyApp/createLogger"));
        Assert.That(result, Contains.Item(">MyApp>Utils"));
        Assert.That(result, Contains.Item(">MyApp>Utils>IFormatter"));
        Assert.That(result, Contains.Item(">MyApp>Utils>IFormatter/format"));
        Assert.That(result, Contains.Item(">MyApp>Utils/formatMessage"));
        Assert.That(result, Contains.Item("+logger"));
    }

    // ... (keep other existing tests)
}