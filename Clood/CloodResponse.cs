namespace Clood;

public class CloodResponse<T> : IHasSuccess
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public T? Data { get; set; }
}

 