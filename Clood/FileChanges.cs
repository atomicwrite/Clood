namespace Clood;

public class FileChanges
{
    public List<FileContent> ChangedFiles { get; set; } = new List<FileContent>();
    public List<FileContent> NewFiles { get; set; } = new List<FileContent>();
}