using System;
using System.IO;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using CloodKey.Interfaces;

namespace CloodKey.KeyTools
{
    public class CmdUtilKeyTool : KeyCli
    {
        private readonly string _cmdUtilPath;

        public CmdUtilKeyTool(string? cmdUtilPath = null)
        {
            _cmdUtilPath = GetCmdUtilPath(cmdUtilPath);
        }

        private string GetCmdUtilPath(string? providedPath)
        {
            if (!string.IsNullOrEmpty(providedPath))
            {
                if (File.Exists(providedPath))
                {
                    return providedPath;
                }
                throw new FileNotFoundException($"The provided cmdutil path does not exist: {providedPath}");
            }

            string? pathFromEnvironment = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathFromEnvironment))
            {
                throw new InvalidOperationException("The PATH environment variable is not set.");
            }

            foreach (string path in pathFromEnvironment.Split(Path.PathSeparator))
            {
                string fullPath = Path.Combine(path, "cmdutil.exe");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            throw new FileNotFoundException("cmdutil.exe could not be found in the system PATH.");
        }

        public override string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            try
            {
                var result = Cli.Wrap(_cmdUtilPath)
                    .WithArguments($"get {key}")
                    .ExecuteBufferedAsync()
                    .GetAwaiter()
                    .GetResult();

                return result.StandardOutput.Trim();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get value for key: {key}", ex);
            }
        }

        public override string Set(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            try
            {
                var result = Cli.Wrap(_cmdUtilPath)
                    .WithArguments($"set {key} {value}")
                    .ExecuteBufferedAsync()
                    .GetAwaiter()
                    .GetResult();

                return result.StandardOutput.Trim();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set value for key: {key}", ex);
            }
        }
    }
}