using Clood.Gits;
using VYaml.Annotations;
using VYaml.Serialization;

namespace Clood.Files
{
    public class CloodFileMap
    {
        private readonly string _folderPath;
        private readonly HashSet<string> _ignoredPaths;
        private GitIgnoreCompiler.CompileResult? _gitParser;

        public CloodFileMap(string folderPath)
        {
            _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
            _ignoredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "node_modules",
                "bin",
                "obj",
                "venv",
                ".venv",
                ".git"
            };
            LoadGitIgnore();
        }

        private void LoadGitIgnore()
        {
            var gitIgnorePath = Path.Combine(_folderPath, ".gitignore");
            if (!File.Exists(gitIgnorePath))
            {
                _gitParser = GitIgnoreCompiler.Compile("");
                return;
            }

            _gitParser = GitIgnoreCompiler.Compile(File.ReadAllText(gitIgnorePath));
        }

        public string CreateYamlMap()
        {
            var fileMap = new Dictionary<string, FilePathInfo>();
            PopulateFileMap(_folderPath, fileMap);


            return YamlSerializer.SerializeToString(fileMap);
        }

        private void PopulateFileMap(string currentPath, Dictionary<string, FilePathInfo> fileMap)
        {
            foreach (var filePath in Directory.EnumerateFiles(currentPath))
            {
                var relativePath = Path.GetRelativePath(_folderPath, filePath);
                if (!IsIgnored(relativePath))
                {
                    fileMap[relativePath] = new FilePathInfo
                    {
                        Size = new FileInfo(filePath).Length,
                        LastModified = File.GetLastWriteTimeUtc(filePath)
                    };
                }
            }

            foreach (var dirPath in Directory.EnumerateDirectories(currentPath))
            {
                var relativePath = Path.GetRelativePath(_folderPath, dirPath);
                if (!IsIgnored(relativePath))
                {
                    PopulateFileMap(dirPath, fileMap);
                }
            }
        }

        private bool IsIgnored(string path)
        {
            if (_gitParser != null && _gitParser.Denies(path))
            {
                return true;
            }

            return _ignoredPaths.Any(ignoredPath =>
                path.StartsWith(ignoredPath, StringComparison.OrdinalIgnoreCase) ||
                path.Contains($"/{ignoredPath}/"));
        }
    }

    [YamlObject]
    public partial struct FilePathInfo
    {
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }
}