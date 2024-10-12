using CliWrap;
using CliWrap.Buffered;
using CloodKey.Interfaces;

namespace CloodKey
{
    public class SecretStore : KeyCli
    {
        private const string PowerShellPath = "powershell.exe";
        private readonly string _vault;

        public SecretStore(string vault,bool autoLockAfterRead = false)
        {
            _vault = vault;
            EnsureVaultExists().GetAwaiter().GetResult();
        }

        public override async Task Delete(string key)
        {
            try
            {
                var result = await Cli.Wrap(PowerShellPath)
                    .WithArguments(new[] { "-Command", $"Set-Secret -Vault {_vault} -Name {key} -Secret ''" })
                    .ExecuteBufferedAsync();

                if (result.ExitCode == 0)
                {
                    return  ;
                }

                throw new KeyNotFoundException($"Key '{key}' not found in vault '{_vault}'. Error: {result.StandardError}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving key '{key}' from vault '{_vault}': {ex.Message}");
                throw;
            }
        }

        private async Task EnsureVaultExists()
        {
            try
            {
                var result = await Cli.Wrap(PowerShellPath)
                    .WithArguments(new[] { "-Command", $"Register-SecretVault -Name {_vault} -ModuleName Microsoft.PowerShell.SecretStore" })
                    .ExecuteBufferedAsync();

                if (result.ExitCode != 0)
                {
                    throw new Exception($"Failed to create vault. Exit code: {result.ExitCode}\nError: {result.StandardError}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating vault: {ex.Message}");
                throw;
            }
        }

        public override async Task<string> Get(string key)
        {
            try
            {
                var result = await Cli.Wrap(PowerShellPath)
                    .WithArguments(new[] { "-Command", $"Get-Secret -Vault {_vault} -Name {key} -AsPlainText" })
                    .ExecuteBufferedAsync();

                if (result.ExitCode == 0)
                {
                    return result.StandardOutput.Trim();
                }
                else
                {
                    throw new KeyNotFoundException($"Key '{key}' not found in vault '{_vault}'. Error: {result.StandardError}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving key '{key}' from vault '{_vault}': {ex.Message}");
                throw;
            }
        }

        public override async Task Set(string key, string value)
        {
            try
            {
                var result = await Cli.Wrap(PowerShellPath)
                    .WithArguments(new[] { "-Command", $"Set-Secret -Vault {_vault} -Name {key} -Secret '{value}'" })
                    .ExecuteBufferedAsync();

                if (result.ExitCode == 0)
                {
                    return;
                }

                throw new Exception($"Failed to set key in vault '{_vault}'. Exit code: {result.ExitCode}\nError: {result.StandardError}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting key '{key}' in vault '{_vault}': {ex.Message}");
                throw;
            }
        }
    }
}