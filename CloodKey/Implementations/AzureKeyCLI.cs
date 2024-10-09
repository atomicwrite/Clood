using System;
using System.Diagnostics;
using CloodKey.Interfaces;

namespace CloodKey.Implementations
{
    public class AzureKeyCLI : IKeyCLI
    {
        public string Get(string key)
        {
            return ExecuteCliCommand($"az keyvault secret show --name {key} --vault-name YourKeyVaultName --query value -o tsv");
        }

        public string Set(string key, string value)
        {
            return ExecuteCliCommand($"az keyvault secret set --name {key} --vault-name YourKeyVaultName --value {value}");
        }

        public bool ValidateCliTool()
        {
            try
            {
                ExecuteCliCommand("az --version");
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
                throw new Exception($"Azure CLI Error: {error}");
            }

            return output.Trim();
        }
    }
}
