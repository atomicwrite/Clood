namespace Clood.Errors;

public class UncommittedFilesException(string message) : Exception(message)
{
}