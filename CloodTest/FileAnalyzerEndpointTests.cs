using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Clood.Endpoints.DTO;
using Newtonsoft.Json;

namespace CloodTest;

[TestFixture]
public class FileAnalyzerEndpointTests
{
    private CloodWebFactory _factory;
    protected HttpClient _client;
    protected string _tempRepoPath;

    [SetUp]
    public void Setup()
    {
        _tempRepoPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        if (string.IsNullOrEmpty(_tempRepoPath))
        {
            throw new Exception("Couldn't get temp repo or empty");
        }

        _factory = new CloodWebFactory("https://localhost:9090", _tempRepoPath);


        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("test",
                "true");
            builder.UseSetting("server",
                "true");
            builder.UseSetting("git-root", _tempRepoPath);
            builder.UseSetting("server-urls",
                "https://localhost:9090");
        });
        _client = _factory.CreateClient();

        // Create a temporary folder for the Git repository

        Directory.CreateDirectory(_tempRepoPath);

        // Initialize Git repository
        RunGitCommand("init");
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
        _client.Dispose();

        // Delete the temporary repository
        if (Directory.Exists(_tempRepoPath))
        {
            Directory.Delete(_tempRepoPath, true);
        }
    }

    private async Task RunGitCommand(string arguments)
    {
        try
        {
            var result = await Cli.Wrap("git")
                .WithArguments(arguments)
                .WithWorkingDirectory(_tempRepoPath)
                .ExecuteBufferedAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Git command failed: {result.StandardError}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to run Git command: {ex.Message}", ex);
        }
    }

    [Test]
    public async Task AnalyzeFiles_WithFileOutsideGitRoot_ShouldFail()
    {
        // Arrange
        var outsideFilePath = Path.Combine(Path.GetTempPath(),
            "outside_file.cs");
        File.WriteAllText(outsideFilePath,
            "public class OutsideClass { }");

        var analyzeRequest = new AnalyzeFilesRequest
        {
            Files = new List<string> { outsideFilePath }
        };

        // Act
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8,
                "application/json"));

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not in the Git root"));
            Assert.That(result.Data, Is.Empty);
        });

        // Cleanup
        File.Delete(outsideFilePath);
    }

    [Test]
    public async Task AnalyzeFiles_WithComplexClassStructure_ShouldReturnCorrectSymbolTree()
    {
        // Arrange
        var code = @"
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

        var filePath = Path.Combine(_tempRepoPath,
            "ComplexClass.cs");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest
        {
            Files = [filePath]
        };

        // Act
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8,
                "application/json"));

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
        });

        var expectedStrings = new List<string>
        {
            "ComplexClass.cs:OuterClass",
            "ComplexClass.cs:OuterClass*OuterProperty",
            "ComplexClass.cs:OuterClass>OuterMethod",
            "ComplexClass.cs:OuterClass>OuterMethod*outerVar",
            "ComplexClass.cs:OuterClass>OuterMethod>LocalMethod1",
            "ComplexClass.cs:OuterClass>OuterMethod>LocalMethod1*localMethod1Var",
            "ComplexClass.cs:OuterClass>OuterMethod>LocalMethod1>NestedLocalMethod",
            "ComplexClass.cs:OuterClass>OuterMethod>LocalMethod1>NestedLocalMethod*nestedVar",
            "ComplexClass.cs:OuterClass>OuterMethod>LocalMethod1*nestedVar",
            "ComplexClass.cs:OuterClass>OuterMethod*anotherOuterVar",
            "ComplexClass.cs:OuterClass>OuterMethod>LocalMethod2",
            "ComplexClass.cs:OuterClass>OuterMethod>LocalMethod2*localMethod2Var",
            "ComplexClass.cs:OuterClass>StaticMethod",
            "ComplexClass.cs:OuterClass>StaticMethod*staticMethodVar",
            "ComplexClass.cs:OuterClass>StaticMethod>StaticLocalMethod",
            "ComplexClass.cs:OuterClass>StaticMethod>StaticLocalMethod*staticLocalVar",
            "ComplexClass.cs:OuterClass>InnerClass",
            "ComplexClass.cs:OuterClass>InnerClass*InnerProperty",
            "ComplexClass.cs:OuterClass>InnerClass>InnerMethod",
            "ComplexClass.cs:OuterClass>InnerClass>InnerMethod*innerVar",
            "ComplexClass.cs:OuterClass>InnerClass>InnerMethod>InnerLocalMethod",
            "ComplexClass.cs:OuterClass>InnerClass>InnerMethod>InnerLocalMethod*innerLocalVar"
        };

        CollectionAssert.AreEqual(expectedStrings, result.Data);
    }
}