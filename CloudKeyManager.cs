using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Threading.Tasks;
using CredentialManagement;

namespace Clood;

public static class CloudKeyManager
{
    // AWS Secrets Manager
    public static async Task SetApiKeyAwsAsync(string apiKey, string secretName, string region)
    {
        var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
        var request = new PutSecretValueRequest
        {
            SecretId = secretName,
            SecretString = apiKey
        };
        await client.PutSecretValueAsync(request);
    }

    public static async Task<string> GetApiKeyAwsAsync(string secretName, string region)
    {
        var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
        var request = new GetSecretValueRequest
        {
            SecretId = secretName
        };
        var response = await client.GetSecretValueAsync(request);
        return response.SecretString;
    }
    public static void SetApiKeyWindowsCredential(string apiKey)
    {
        using (var cred = new Credential())
        {
            cred.Target = "CloodApiKey";
            cred.Username = "CloodUser";
            cred.Password = apiKey;
            cred.Type = CredentialType.Generic;
            cred.PersistanceType = PersistanceType.LocalComputer;
            cred.Save();
        }
    }

    public static string GetApiKeyWindowsCredential()
    {
        using (var cred = new Credential())
        {
            cred.Target = "CloodApiKey";
            if (cred.Load())
            {
                return cred.Password;
            }
            return null;
        }
    }
    // Azure Key Vault
    public static async Task SetApiKeyAzureAsync(string apiKey, string keyVaultName, string secretName)
    {
        var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), new DefaultAzureCredential());
        await client.SetSecretAsync(secretName, apiKey);
    }

    public static async Task<string> GetApiKeyAzureAsync(string keyVaultName, string secretName)
    {
        var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), new DefaultAzureCredential());
        var secret = await client.GetSecretAsync(secretName);
        return secret.Value.Value;
    }
}