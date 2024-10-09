namespace Clood;

public class CloodResponse<T>
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public T Data { get; set; }
}

public class CloodResponse
{
    public string Id { get; set; }
    public string NewBranch { get; set; }
    public FileChanges ProposedChanges { get; set; }
}