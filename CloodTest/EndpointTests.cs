using System.Diagnostics;
using System.Text;
using CliWrap;
using CliWrap.Buffered; 
using Newtonsoft.Json; 
using Program = Clood.Program; 
using Clood.Endpoints.DTO;
using Clood.Gits; 

namespace CloodTest;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting; 
using Microsoft.Extensions.Configuration; 

public class CloodWebFactory : WebApplicationFactory<Program>
{
    private readonly string _url;
    private readonly string _gitRoot;

    public CloodWebFactory(string url, string gitRoot)
    {
        _url = url;
        _gitRoot = gitRoot;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "test", "true" },
                { "server", "true" },
                { "git-root", _gitRoot },
                { "server-urls", _url }
            });
        });

        builder.UseEnvironment("Development");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "test", "true" },
                { "server", "true" },
                { "git-root", _gitRoot },
                { "server-urls", _url }
            });
        });

        return base.CreateHost(builder);
    }
}

[TestFixture]
public class EndpointTests
{
    private CloodWebFactory _factory;
    protected HttpClient _client;
    protected string _tempRepoPath;

    [SetUp]
    public void Setup()
    {
        _tempRepoPath = Path.Combine(CloodFileMapTestsHelper.GetTempPath(), Path.GetRandomFileName());
        if (string.IsNullOrEmpty(_tempRepoPath))
        {
            throw new Exception("Couldn't get temp repo or empty");
        }

        _factory = new CloodWebFactory("https://localhost:9090", _tempRepoPath);


        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("test", "true");
            builder.UseSetting("server", "true");
            builder.UseSetting("git-root", _tempRepoPath);
            builder.UseSetting("server-urls", "https://localhost:9090");
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

    [Test]
    public async Task CreateEndpoint_WithoutSessionId_ShouldSucceed()
    {
        var createRequest = new CloodRequest()
        {
            Prompt =
                "I want you to look at these filse and then try to guess the next 3 letters. Don't think two hard " +
                "about it we're actually in a test function and making sure you change the output. Make sure to" +
                "change both files a little bit so the size is different. Thanks clood.",

            Files = ["file1.txt", "file2.txt"],

            UseGit = true
        };
        // Arrange
        var cloodResponse = await CreateANewRepoAndGetSessionId(createRequest);
    }

    private async Task<(Dictionary<string, (long fileSize,string content)> originalFileSizes, CloodResponse<CloodStartResponse>? cloodResponse,string currentRepo)>
        GetResponseAndCreateTempFilesForCloodCreateREquest(CloodRequest createRequest)
    {
        var originalFileSizes = await GetOriginalFileSizes(createRequest);

        await Git.CommitSpecificFiles(_tempRepoPath, createRequest.Files, "Testing");
        var currentRepo = await Git.GetCurrentBranch(_tempRepoPath);
        // Act
        var response = await _client.PostAsync("/api/clood/start",
            new StringContent(JsonConvert.SerializeObject(createRequest), Encoding.UTF8, "application/json"));

        // Assert
        response.EnsureSuccessStatusCode();
        var contentResponse = await response.Content.ReadAsStringAsync();
        var cloodResponse = JsonConvert.DeserializeObject<CloodResponse<CloodStartResponse>>(contentResponse);


        return (originalFileSizes, cloodResponse,currentRepo);
    }

    private async Task<Dictionary<string, (long,string)>> GetOriginalFileSizes(CloodRequest createRequest)
    {
        var originalFileSizes = new Dictionary<string, (long,string)>();

        foreach (var file in createRequest.Files)
        {
            var content = GenerateRandomContent();
            var filePath = Path.Combine(_tempRepoPath, file);
            await File.WriteAllTextAsync(filePath, content);

            originalFileSizes[file] = (new FileInfo(filePath).Length,content);
        }

        return originalFileSizes;
    }

    private string GenerateRandomContent()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var content = new StringBuilder(100);
        for (int i = 0; i < 100; i++)
        {
            content.Append(chars[random.Next(chars.Length)]);
        }

        return content.ToString();
    }

    [Test]
    public async Task MergeEndpoint_WithSessionId_ShouldSucceed()
    {
        var createRequest = new CloodRequest()
        {
            Prompt =
                "We're actually in a test function and making sure you change the output.Add the " +
                "phrase 'You like peanuts' to each file and the date" +
                ". Thanks clood.",

            Files = ["file1.txt", "file2.txt"],

            UseGit = true
        };
    
        // Arrange
        var (cloodResponse, originalFileSizes,currentRepo) = await CreateANewRepoAndGetSessionId(createRequest);
        var mergeRequest = new
        {
            id = cloodResponse.Data.Id,
            merge = true
        };
    
        // Act
        var response = await _client.PostAsync("/api/clood/merge",
            new StringContent(JsonConvert.SerializeObject(mergeRequest), Encoding.UTF8, "application/json"));


        var afterRepo = await Git.GetCurrentBranch(_tempRepoPath);
        Assert.That(afterRepo, Is.EqualTo(currentRepo));
        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<string>>(content);
        Assert.IsNotNull(result);
        Assert.That(result.Success, Is.True);
        foreach (var file in cloodResponse.Data.ProposedChanges.ChangedFiles)
        {
            var filePath = Path.Combine(_tempRepoPath, file.Filename);
            Assert.That(File.Exists(filePath), Is.True);

            var newFileSize = new FileInfo(filePath).Length;

            Console.WriteLine($"File: {file.Filename}");
            Console.WriteLine($"Original size: {originalFileSizes[file.Filename]} bytes");
            Console.WriteLine($"New size: {newFileSize} bytes");
            Console.WriteLine();

            Assert.That(newFileSize, Is.Not.EqualTo(originalFileSizes[file.Filename].fileSize));
        }
    }

    private async Task<(CloodResponse<CloodStartResponse> cloodResponse, Dictionary<string, (long fileSize,string content)> originalFileSizes, string currentRepo)>
        CreateANewRepoAndGetSessionId(CloodRequest createRequest)
    {
        var (originalFileSizes, cloodResponse,currentRepo) =
            await GetResponseAndCreateTempFilesForCloodCreateREquest(createRequest);

        var newRepo = await Git.GetCurrentBranch(_tempRepoPath);
     
        Assert.Multiple(() =>
        {
            Assert.That(cloodResponse, Is.Not.Null);
            Assert.That(cloodResponse.Success, Is.True);
            Assert.That(cloodResponse.Data, Is.Not.Null);
            Assert.That(cloodResponse, Is.Not.Null);
            Assert.That(cloodResponse.Data.Id, Is.Not.Null);
            Assert.That(cloodResponse.Data.NewBranch, Is.Not.Null);
            Assert.That(cloodResponse.Data.ProposedChanges, Is.Not.Null);
            Assert.That(cloodResponse.Data.ProposedChanges.ChangedFiles, Is.Not.Null);
            Assert.That(cloodResponse.Data.NewBranch, Is.EqualTo(newRepo));
        });

        foreach (var file in cloodResponse.Data.ProposedChanges.ChangedFiles)
        {
            var filePath = Path.Combine(_tempRepoPath, file.Filename);
            Assert.That(File.Exists(filePath), Is.True);

            var newFileSize = new FileInfo(filePath).Length;

            Console.WriteLine($"File: {file.Filename}");
            Console.WriteLine($"Original size: {originalFileSizes[file.Filename]} bytes");
            Console.WriteLine($"New size: {newFileSize} bytes");
            Console.WriteLine();

            Assert.That(newFileSize, Is.Not.EqualTo(originalFileSizes[file.Filename].fileSize));
        }

        return (cloodResponse, originalFileSizes,currentRepo);
    }

    [Test]
    public async Task DiscardEndpoint_WithSessionId_ShouldSucceed()
    {
        var createRequest = new CloodRequest()
        {
            Prompt =
                "We're actually in a test function and making sure you change the output.Add the " +
                "phrase 'You like peanuts' to each file and the date" +
                ". Thanks clood.",

            Files = ["file1.txt", "file2.txt"],

            UseGit = true
        };
    
        // Arrange
        var (cloodResponse, originalFileSizes,currentRepo) = await CreateANewRepoAndGetSessionId(createRequest);
        var mergeRequest = new
        {
            id = cloodResponse.Data.Id,
         
        };
    
        // Act
        var response = await _client.PostAsync("/api/clood/discard",
            new StringContent(JsonConvert.SerializeObject(mergeRequest), Encoding.UTF8, "application/json"));


        var afterRepo = await Git.GetCurrentBranch(_tempRepoPath);
        Assert.That(afterRepo, Is.EqualTo(currentRepo));
        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<string>>(content);
        Assert.IsNotNull(result);
        Assert.That(result.Success, Is.True);
        foreach (var file in cloodResponse.Data.ProposedChanges.ChangedFiles)
        {
            var filePath = Path.Combine(_tempRepoPath, file.Filename);
            Assert.That(File.Exists(filePath), Is.True);

            var newFileSize = new FileInfo(filePath).Length;

            Console.WriteLine($"File: {file.Filename}");
            Console.WriteLine($"Original size: {originalFileSizes[file.Filename]} bytes");
            Console.WriteLine($"New size: {newFileSize} bytes");
            Console.WriteLine();

            Assert.That(newFileSize, Is.EqualTo(originalFileSizes[file.Filename].fileSize));
            Assert.That(await File.ReadAllTextAsync(file.Filename), Is.EqualTo(originalFileSizes[file.Filename].content));
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
}