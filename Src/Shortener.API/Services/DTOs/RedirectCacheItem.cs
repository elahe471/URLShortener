namespace Shortener.API.Services.DTOs
{
    public sealed record RedirectCacheItem(
     string DestinationURL,
     DateTime ExpirationDate);
}
