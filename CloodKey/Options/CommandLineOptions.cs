using CommandLine;

namespace CloodKey.Options
{
    public class CommandLineOptions
    {
        [Option('s', "store", Required = true, HelpText = "Credential store to use (aws, azure, or os)")]
        public string CredentialStore { get; set; } = string.Empty;

        [Option('o', "operation", Required = true, HelpText = "Operation to perform (get or set)")]
        public string Operation { get; set; } = string.Empty;

        [Option('k', "key", Required = true, HelpText = "Key for the secret")]
        public string Key { get; set; } = string.Empty;

        [Option('v', "value", Required = false, HelpText = "Value to set (required for set operation)")]
        public string? Value { get; set; }
    }
}
