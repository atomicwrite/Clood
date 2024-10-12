namespace Clood.Endpoints;

public class CloodPromptRequest
{
    public string Prompt { get; set; } = string.Empty;
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<string> Files { get; set; } = [];
}