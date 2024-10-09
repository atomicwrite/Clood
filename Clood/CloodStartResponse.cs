namespace Clood;

public class CloodStartResponse
{
    public string Id { get; set; }
    public string NewBranch { get; set; }
    public FileChanges ProposedChanges { get; set; }
}