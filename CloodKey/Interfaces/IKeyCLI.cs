namespace CloodKey.Interfaces
{
    public interface IKeyCLI
    {
        string Get(string key);
        string Set(string key, string value);
        bool ValidateCliTool();
    }
}
