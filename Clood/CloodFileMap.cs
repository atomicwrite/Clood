using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VYaml.Annotations;
using VYaml.Serialization;
using VYaml.Emitter;

namespace Clood
{
    public class CloodFileMap
    {
        private readonly string _folderPath;
        private readonly HashSet<string> _ignoredPaths;

        public CloodFileMap(string folderPath)
        {
            _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
            _ignoredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "node_modules",
                "bin",
                "obj",
                "venv",
                ".venv"
                
            };
            LoadGitIgnore();
        }

        private void LoadGitIgnore()
        {
            var gitIgnorePath = Path.Combine(_folderPath, ".gitignore");
            if (!File.Exists(gitIgnorePath)) return;
            foreach (var line in File.ReadLines(gitIgnorePath))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#")) continue;
                if (trimmedLine.EndsWith("/") || trimmedLine.EndsWith("/*"))
                {
                    _ignoredPaths.Add(trimmedLine);
                }
            }
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
            return _ignoredPaths.Any(ignoredPath => path.StartsWith(ignoredPath, StringComparison.OrdinalIgnoreCase));
        }
    }
    [YamlObject]
    public partial struct FilePathInfo
    {
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }
}