namespace Shortener.API.Exceptions;


public sealed class ShortCodeGenerationException : Exception
{
    public ShortCodeGenerationException()
        : base("Unable to generate a unique shortened code.")
    {
    }
}
