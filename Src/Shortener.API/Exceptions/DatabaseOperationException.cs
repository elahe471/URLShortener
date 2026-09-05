namespace Shortener.API.Exceptions;

public sealed class DatabaseOperationException : Exception
{
    public DatabaseOperationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
