namespace Clood.Endpoints.API;

public class InvalidFilePathException : Exception
{
    public InvalidFilePathException(string message) : base(message) { }
}