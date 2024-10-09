using System;
using System.Diagnostics;
using CloodKey.Interfaces;

namespace CloodKey.Implementations
{
    public class AwsKeyCLI : IKeyCLI
    {
        public string Get(string key)
        {
            return ExecuteCliCommand($"aws secretsmanager get-secret-value --secret-id {key} --query SecretString --output text");
        }

        public string Set(string key, string value)
        {
            return ExecuteCliCommand($"aws secretsmanager create-secret --name {key} --secret-string {value}");
        }

        public bool ValidateCliTool()
        {
            try
            {
                ExecuteCliCommand("aws --version");
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
                throw new Exception($"AWS CLI Error: {error}");
            }

            return output.Trim();
        }
    }
}
