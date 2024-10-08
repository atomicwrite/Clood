using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Clood;

public static class ExpandedApiKeyManager
{
    private const string ConfigFileName = "clood_config.json";
    private static readonly byte[] Entropy = Encoding.Unicode.GetBytes("CloodSecureConfig");

    // Option 1: Encrypted File Storage
    public static void SetApiKeyFile(string apiKey)
    {
        var config = new Dictionary<string, string>
        {
            ["EncryptedApiKey"] = EncryptString(apiKey)
        };

        File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(config));
    }

    public static string GetApiKeyFile()
    {
        if (!File.Exists(ConfigFileName))
        {
            return null;
        }

        var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(ConfigFileName));
        return config?.TryGetValue("EncryptedApiKey", out var encryptedApiKey) == true
            ? DecryptString(encryptedApiKey)
            : null;
    }

    private static string EncryptString(string input)
    {
        byte[] encryptedData = ProtectedData.Protect(
            Encoding.Unicode.GetBytes(input),
            Entropy,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedData);
    }

    private static string DecryptString(string encryptedData)
    {
        byte[] decryptedData = ProtectedData.Unprotect(
            Convert.FromBase64String(encryptedData),
            Entropy,
            DataProtectionScope.CurrentUser);
        return Encoding.Unicode.GetString(decryptedData);
    }
}