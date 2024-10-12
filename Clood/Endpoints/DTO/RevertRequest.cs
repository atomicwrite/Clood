// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Clood.Endpoints.DTO;

public class RevertRequest : IHasIdString
{
    public string Id { get; set; }= default!;
    
}