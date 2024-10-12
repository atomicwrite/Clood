// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Clood.Endpoints.DTO;

public class MergeRequest : IHasIdString
{
    public string Id { get; set; }= default!;
    
}