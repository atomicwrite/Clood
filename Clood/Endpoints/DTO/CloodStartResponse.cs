// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
using Clood.Files;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Clood.Endpoints.DTO;

public class CloodStartResponse
{
    public string Id { get; set; }= default!;
    public string NewBranch { get; set; }= default!;
    public FileChanges ProposedChanges { get; set; } = default!;
}