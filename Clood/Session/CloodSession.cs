// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
using Clood.Files;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Clood.Session;

public class CloodSession
{
    public string Id { get; set; }= default!;
    public bool UseGit { get; set; }
    public string OriginalBranch { get; set; }= default!;
    public string NewBranch { get; set; }= default!;
    public string GitRoot { get; set; }= default!;
    public List<string> Files { get; set; }= default!;
    public FileChanges ProposedChanges { get; set; }= default!;
}