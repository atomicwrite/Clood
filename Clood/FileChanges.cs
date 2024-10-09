using System.Text.Json.Serialization;

namespace Clood;

public class FileChanges
{
    [JsonPropertyName("changedFiled")] public List<FileContent> ChangedFiles { get; set; } = [];
    [JsonPropertyName("newFiles")] public List<FileContent> NewFiles { get; set; } = [];
    [JsonPropertyName("answered")] public bool Answered { get; set; } = false;
}