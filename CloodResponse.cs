namespace Clood;

public class CloodResponse
{
    public string Id { get; set; }
    public string NewBranch { get; set; }
    public Dictionary<string, string> ProposedChanges { get; set; }
}