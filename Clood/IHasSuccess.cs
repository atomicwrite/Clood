namespace Clood;

public interface IHasSuccess
{
    bool Success { get; set; }
    string? ErrorMessage { get; set; }
}