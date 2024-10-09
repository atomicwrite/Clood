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
                var arguments = new[] { "/list:" + key };
                var result = Cli.Wrap(CmdKeyPath)
                    .WithArguments(arguments)
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

                Console.WriteLine($"Command sent: {CmdKeyPath} {string.Join(" ", arguments)}");
                Console.WriteLine($"Program output:\n{result.StandardOutput}");
                throw new KeyNotFoundException($"Key '{key}' not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving key '{key}': {ex.Message}");
                throw;
            }
        }

        public override string Set(string key, string value)
        {
            try
            {
                var arguments = new[] { $"/add:{key}", $"/pass:{value}" };
                var result = Cli.Wrap(CmdKeyPath)
                    .WithArguments(arguments)
                    .ExecuteBufferedAsync()
                    .GetAwaiter()
                    .GetResult();

                if (result.ExitCode == 0)
                {
                    return "Key set successfully.";
                }
                else
                {
                    Console.WriteLine($"Command sent: {CmdKeyPath} {string.Join(" ", arguments)}");
                    Console.WriteLine($"Program output:\n{result.StandardOutput}");
                    Console.WriteLine($"Error output:\n{result.StandardError}");
                    throw new Exception($"Failed to set key. Exit code: {result.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting key '{key}': {ex.Message}");
                throw;
            }
        }
    }
}