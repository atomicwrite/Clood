using System;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using CloodKey.Interfaces;

namespace CloodKey.KeyTools
{
    public class CmdUtilKeyTool : KeyCli
    {
        private readonly string _cmdUtilPath;

        public CmdUtilKeyTool(string cmdUtilPath)
        {
            _cmdUtilPath = cmdUtilPath ?? throw new ArgumentNullException(nameof(cmdUtilPath));
        }

        public override string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            try
            {
                var result = Cli.Wrap(_cmdUtilPath)
                    .WithArguments($"get {key}")
                    .ExecuteBufferedAsync()
                    .GetAwaiter()
                    .GetResult();

                return result.StandardOutput.Trim();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get value for key: {key}", ex);
            }
        }

        public override string Set(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            try
            {
                var result = Cli.Wrap(_cmdUtilPath)
                    .WithArguments($"set {key} {value}")
                    .ExecuteBufferedAsync()
                    .GetAwaiter()
                    .GetResult();

                return result.StandardOutput.Trim();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set value for key: {key}", ex);
            }
        }
    }
}