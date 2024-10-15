using System.Text.Json.Serialization;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Clood.Files;

public class FileChanges
{
    [JsonPropertyName("changedFiled")] public List<FileContent>? ChangedFiles { get; set; } = [];
    [JsonPropertyName("newFiles")] public List<FileContent>? NewFiles { get; set; } = [];
    [JsonPropertyName("answered")] public bool Answered { get; set; }  
}


public class PromptImprovement
{
    [JsonPropertyName("improvedPrompt")] public string improvedPrompt { get; set; } = "";
    [JsonPropertyName("answered")] public bool answered { get; set; }  
}