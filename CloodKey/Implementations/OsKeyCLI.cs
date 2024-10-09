using System;
using System.Diagnostics;
using CloodKey.Interfaces;

namespace CloodKey.Implementations
{
    public class OsKeyCLI : IKeyCLI
    {
        public string Get(string key)
        {
            return ExecuteCliCommand($"cmdkey /generic:{key} /pass");
        }

        public string Set(string key, string value)
        {
            return ExecuteCliCommand($"cmdkey /generic:{key} /user:CloodKey /pass:{value}");
        }

        public bool ValidateCliTool()
        {
            try
            {
                ExecuteCliCommand("cmdkey /?");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string ExecuteCliCommand(string command)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception($"OS CLI Error: {error}");
            }

            return output.Trim();
        }
    }
}
