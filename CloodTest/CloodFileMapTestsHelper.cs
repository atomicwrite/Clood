namespace CloodTest;

public static class CloodFileMapTestsHelper
{
    public static string GetTempPath()
    {
        var environmentVariable = Environment.GetEnvironmentVariable("RUNNER_TEMP");
        if (environmentVariable != null)
        {
            environmentVariable += environmentVariable + "/tmp";
        }
        return environmentVariable ?? Path.GetTempPath();
    }
}