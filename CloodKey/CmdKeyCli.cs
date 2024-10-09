using System;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using CloodKey.Interfaces;

namespace CloodKey
{
    public class CmdKeyCli : KeyCli
    {
        private const string CmdKeyPath = "cmdkey.exe";

        public override string Get(string key)
        {
            try
            {
                var result = Cli.Wrap(CmdKeyPath)
                    .WithArguments(new[] { "/list:" + key })
                    .ExecuteBufferedAsync()
                    .GetAwaiter()
                    .GetResult();

                // Parse the output to extract the value
                // This is a simplified example and may need to be adjusted based on the actual output format
                var lines = result.StandardOutput.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("Password:"))
                    {
                        return line.Substring("Password:".Length).Trim();
                    }
                }

                throw new KeyNotFoundException($"Key '{key}' not found.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving key '{key}': {ex.Message}", ex);
            }
        }

        public override string Set(string key, string value)
        {
            try
            {
                var result = Cli.Wrap(CmdKeyPath)
                    .WithArguments(new[] { $"/add:{key}", $"/pass:{value}" })
                    .ExecuteBufferedAsync()
                    .GetAwaiter()
                    .GetResult();

                if (result.ExitCode == 0)
                {
                    return "Key set successfully.";
                }
                else
                {
                    throw new Exception($"Failed to set key. Exit code: {result.ExitCode}. Error: {result.StandardError}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error setting key '{key}': {ex.Message}", ex);
            }
        }
    }
}