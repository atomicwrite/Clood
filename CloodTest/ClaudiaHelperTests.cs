 
using Clood;

namespace CloodTest
{
    [TestFixture]
    public class ClaudiaHelperTests
    {
        private string rootFolder;
        private List<string> testFiles;

        [SetUp]
        public void Setup()
        {
            rootFolder = Path.Combine(Path.GetTempPath(), "ClaudiaHelperTest");
            Directory.CreateDirectory(rootFolder);

            testFiles = new List<string>
            {
                Path.Combine(rootFolder, "file1.txt"),
                Path.Combine(rootFolder, "file2.txt")
            };

            File.WriteAllText(testFiles[0], "Content of file 1");
            File.WriteAllText(testFiles[1], "Content of file 2");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var file in testFiles)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }

            if (Directory.Exists(rootFolder))
                Directory.Delete(rootFolder, true);
        }

        [Test]
        public async Task SendRequestToClaudia_ValidInput_ReturnsNonNullResponse()
        {
        
            // Arrange
            string prompt = "Summerize german politics over the last 100 years";
            string systemPrompt = "You are a helpful AI assistant.";

            // Act
            string? result = await ClaudiaHelper.SendRequestToClaudia(prompt, rootFolder, systemPrompt, testFiles);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.GreaterThan(0));

            // You might want to add more specific assertions based on the expected content of the response
            Assert.That(result, Does.Contain("file"), "Response should mention 'file' as it's summarizing file contents");
        }
    }
}