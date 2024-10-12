using Clood.Files;

namespace CloodTest
{
    [TestFixture]
    public class CloodFileMapTests
    {
        private string _testFolderPath;

        [SetUp]
        public void Setup()
        {
            _testFolderPath = Path.Combine(Path.GetTempPath(), "CloodFileMapTest");
            Directory.CreateDirectory(_testFolderPath);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_testFolderPath, true);
        }

        [Test]
        public void CreateYamlMap_EmptyFolder_ReturnsEmptyYaml()
        {
            var cloodFileMap = new CloodFileMap(_testFolderPath);
            var yamlMap = cloodFileMap.CreateYamlMap();

            Assert.That(yamlMap, Is.EqualTo("{}"));
        }

        [Test]
        public void CreateYamlMap_WithFiles_ReturnsCorrectYaml()
        {
            // Arrange
            var file1Path = Path.Combine(_testFolderPath, "file1.txt");
            var file2Path = Path.Combine(_testFolderPath, "file2.txt");
            File.WriteAllText(file1Path, "Test content 1");
            File.WriteAllText(file2Path, "Test content 2");

            var cloodFileMap = new CloodFileMap(_testFolderPath);

            // Act
            var yamlMap = cloodFileMap.CreateYamlMap();

            // Assert
            Assert.That(yamlMap, Does.Contain("file1.txt:"));
            Assert.That(yamlMap, Does.Contain("file2.txt:"));
            Assert.That(yamlMap, Does.Contain("size:"));
            Assert.That(yamlMap, Does.Contain("lastModified:"));
        }

        [Test]
        public void CreateYamlMap_WithIgnoredFiles_ExcludesIgnoredFiles()
        {
            // Arrange
            var file1Path = Path.Combine(_testFolderPath, "file1.txt");
            var ignoredFilePath = Path.Combine(_testFolderPath, "node_modules", "ignored.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(ignoredFilePath)!);
            File.WriteAllText(file1Path, "Test content 1");
            File.WriteAllText(ignoredFilePath, "Ignored content");

            var cloodFileMap = new CloodFileMap(_testFolderPath);

            // Act
            var yamlMap = cloodFileMap.CreateYamlMap();

            // Assert
            Assert.That(yamlMap, Does.Contain("file1.txt:"));
            Assert.That(yamlMap, Does.Not.Contain("ignored.txt"));
        }
    }
}
