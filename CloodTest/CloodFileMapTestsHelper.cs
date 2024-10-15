namespace CloodTest;

public static class CloodFileMapTestsHelper
{
    public static string GetTempPath()
    {
        var environmentVariable = Environment.GetEnvironmentVariable("RUNNER_TEMP");

        return environmentVariable ?? Path.GetTempPath();
    }
}