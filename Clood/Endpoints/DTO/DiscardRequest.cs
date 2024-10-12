// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Clood.Endpoints.DTO;

public class DiscardRequest : IHasIdString
{
    public string Id { get; set; }= default!;
    
}