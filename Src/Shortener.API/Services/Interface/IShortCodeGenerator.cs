namespace Shortener.API.Services.Interface
{
    public interface IShortCodeGenerator
    {
        string Generate(long sequence);
    }
}
