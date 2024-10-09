using System;
using System.IO;

namespace CloodKey
{
    public class CredentialManager
    {
        private readonly string? _azurePath;
        private readonly string? _awsPath;
        private readonly string? _windowsPath;

        public CredentialManager(string? azurePath, string? awsPath, string? windowsPath)
        {
            _azurePath = azurePath;
            _awsPath = awsPath;
            _windowsPath = windowsPath;
        }

        public string? GetCredential(string provider, string key)
        {
            return provider.ToLower() switch
            {
                "azure" => GetAzureCredential(key),
                "aws" => GetAwsCredential(key),
                "windows" => GetWindowsCredential(key),
                _ => throw new ArgumentException("Invalid provider", nameof(provider))
            };
        }

        public void SetCredential(string provider, string key, string value)
        {
            switch (provider.ToLower())
            {
                case "azure":
                    SetAzureCredential(key, value);
                    break;
                case "aws":
                    SetAwsCredential(key, value);
                    break;
                case "windows":
                    SetWindowsCredential(key, value);
                    break;
                default:
                    throw new ArgumentException("Invalid provider", nameof(provider));
            }
        }

        private string? GetAzureCredential(string key)
        {
            // Implement Azure credential retrieval logic here
            // This is a placeholder implementation
            if (string.IsNullOrEmpty(_azurePath))
            {
                throw new InvalidOperationException("Azure credentials path not set");
            }
            var credentialFile = Path.Combine(_azurePath, "credentials.txt");
            var lines = File.ReadAllLines(credentialFile);
            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key)
                {
                    return parts[1].Trim();
                }
            }
            return null;
        }

        private void SetAzureCredential(string key, string value)
        {
            // Implement Azure credential setting logic here
            // This is a placeholder implementation
            if (string.IsNullOrEmpty(_azurePath))
            {
                throw new InvalidOperationException("Azure credentials path not set");
            }
            var credentialFile = Path.Combine(_azurePath, "credentials.txt");
            var lines = File.Exists(credentialFile) ? File.ReadAllLines(credentialFile) : Array.Empty<string>();
            var updatedLines = new List<string>();
            bool found = false;
            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key)
                {
                    updatedLines.Add($"{key}={value}");
                    found = true;
                }
                else
                {
                    updatedLines.Add(line);
                }
            }
            if (!found)
            {
                updatedLines.Add($"{key}={value}");
            }
            File.WriteAllLines(credentialFile, updatedLines);
        }

        private string? GetAwsCredential(string key)
        {
            // Implement AWS credential retrieval logic here
            // This is a placeholder implementation
            if (string.IsNullOrEmpty(_awsPath))
            {
                throw new InvalidOperationException("AWS credentials path not set");
            }
            var credentialFile = Path.Combine(_awsPath, "credentials");
            var lines = File.ReadAllLines(credentialFile);
            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key)
                {
                    return parts[1].Trim();
                }
            }
            return null;
        }

        private void SetAwsCredential(string key, string value)
        {
            // Implement AWS credential setting logic here
            // This is a placeholder implementation
            if (string.IsNullOrEmpty(_awsPath))
            {
                throw new InvalidOperationException("AWS credentials path not set");
            }
            var credentialFile = Path.Combine(_awsPath, "credentials");
            var lines = File.Exists(credentialFile) ? File.ReadAllLines(credentialFile) : Array.Empty<string>();
            var updatedLines = new List<string>();
            bool found = false;
            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key)
                {
                    updatedLines.Add($"{key} = {value}");
                    found = true;
                }
                else
                {
                    updatedLines.Add(line);
                }
            }
            if (!found)
            {
                updatedLines.Add($"{key} = {value}");
            }
            File.WriteAllLines(credentialFile, updatedLines);
        }

        private string? GetWindowsCredential(string key)
        {
            // Implement Windows credential retrieval logic here
            // This is a placeholder implementation
            if (string.IsNullOrEmpty(_windowsPath))
            {
                throw new InvalidOperationException("Windows credentials path not set");
            }
            var credentialFile = Path.Combine(_windowsPath, "credentials.xml");
            // In a real implementation, you would use the Windows Credential Manager API
            // For this example, we'll use a simple XML file
            var xml = File.ReadAllText(credentialFile);
            var startTag = $"<{key}>";
            var endTag = $"</{key}>";
            var startIndex = xml.IndexOf(startTag);
            var endIndex = xml.IndexOf(endTag);
            if (startIndex != -1 && endIndex != -1)
            {
                startIndex += startTag.Length;
                return xml.Substring(startIndex, endIndex - startIndex);
            }
            return null;
        }

        private void SetWindowsCredential(string key, string value)
        {
            // Implement Windows credential setting logic here
            // This is a placeholder implementation
            if (string.IsNullOrEmpty(_windowsPath))
            {
                throw new InvalidOperationException("Windows credentials path not set");
            }
            var credentialFile = Path.Combine(_windowsPath, "credentials.xml");
            // In a real implementation, you would use the Windows Credential Manager API
            // For this example, we'll use a simple XML file
            string xml;
            if (File.Exists(credentialFile))
            {
                xml = File.ReadAllText(credentialFile);
            }
            else
            {
                xml = "<credentials></credentials>";
            }
            var startTag = $"<{key}>";
            var endTag = $"</{key}>";
            var startIndex = xml.IndexOf(startTag);
            var endIndex = xml.IndexOf(endTag);
            if (startIndex != -1 && endIndex != -1)
            {
                xml = xml.Remove(startIndex, endIndex + endTag.Length - startIndex);
            }
            xml = xml.Insert(xml.IndexOf("</credentials>"), $"<{key}>{value}</{key}>");
            File.WriteAllText(credentialFile, xml);
        }
    }
}