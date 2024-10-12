// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Clood.Endpoints.DTO;

public class CloodRequest
{
    public string Prompt { get; set; } = default!;

    public List<string> Files { get; set; } = [];
 
    public bool UseGit { get; set; } = true; // Default to true for backward compatibility
}