using Clood.Helpers;

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

            testFiles =
            [
                Path.Combine(rootFolder, "file1.txt"),
                Path.Combine(rootFolder, "file2.txt")
            ];

            File.WriteAllText(testFiles[0], "Content of file 1");
            File.WriteAllText(testFiles[1], "Content of file 2");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var file in testFiles.Where(File.Exists))
            {
                File.Delete(file);
            }

            if (Directory.Exists(rootFolder))
                Directory.Delete(rootFolder, true);
        }

        [Test]
        public async Task SendRequestToClaudia_ValidInput_ReturnsNonNullResponse()
        {
        
            // Arrange
            const string prompt = "Summerize german politics over the last 100 years";
       

            // Act
            var result = await ClaudiaHelper.SendRequestToClaudia(prompt, rootFolder, testFiles);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            
        }
    }
}