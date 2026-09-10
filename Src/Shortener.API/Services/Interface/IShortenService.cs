namespace Shortener.API.Services.Interface
{
    public interface IShortenService
    {
        Task<string> ShortenUrlAsync(
         string longUrl,
         DateTimeOffset expirationDate,
         CancellationToken cancellationToken);
    }
}
