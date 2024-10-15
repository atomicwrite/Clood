namespace CloodTest;

public static class CloodFileMapTestsHelper
{
    public static string GetTempPath()
    {
        var environmentVariable = Environment.GetEnvironmentVariable("RUNNER_TEMP");
        if (environmentVariable != null)
        {
            var random = Random.Shared.Next();
            environmentVariable += environmentVariable + $"/tmp{random}/";
        }
        return environmentVariable ?? Path.GetTempPath();
    }
}