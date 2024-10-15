// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Clood.Endpoints.DTO;

public class MergeRequest : IHasIdString
{
    public string Id { get; set; }= default!;
    
}
public class AnalyzeFilesRequest
{
    public List<string> Files { get; set; } = new List<string>();
}