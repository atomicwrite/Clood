namespace Clood;

public class CloodSession
{
    public string Id { get; set; }
    public bool UseGit { get; set; }
    public string OriginalBranch { get; set; }
    public string NewBranch { get; set; }
    public string GitRoot { get; set; }
    public List<string> Files { get; set; }
    public FileChanges ProposedChanges { get; set; }
}