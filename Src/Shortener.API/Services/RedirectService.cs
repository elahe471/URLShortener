using Microsoft.Extensions.Caching.Hybrid;
using Shortener.API.Services.DTOs;

namespace Shortener.API.Services;

public sealed class RedirectService(
    ShortenerURLContext context,
    TimeProvider timeProvider,
    HybridCache cache,
    IOptions<CacheSettings> cacheSettings,
    ILogger<RedirectService> logger)
    : IRedirectService
{
    private readonly ShortenerURLContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IOptions<CacheSettings> _cacheSettings = cacheSettings;
    private readonly HybridCache _cache = cache;
    private readonly ILogger<RedirectService> _logger = logger;

    public async Task<Result<string>> ResolveAsync(string shortCode,CancellationToken cancellationToken)
    {
        RedirectCacheItem? urlTag;

        if (_cacheSettings.Value.UseCache)
        {
            var cacheKey = $"r:{shortCode}";

            urlTag = await _cache.GetOrCreateAsync(
                cacheKey,
                async cancel =>
                {
                    _logger.LogDebug("Cache miss for {ShortCode}. Reading from MongoDB.",shortCode);

                    return await GetFromDatabaseAsync(shortCode,cancel);
                },
                cancellationToken: cancellationToken);
        }
        else
        {
            urlTag = await GetFromDatabaseAsync(shortCode,cancellationToken);
        }

        if (urlTag is null)
        {
            return Result<string>.Failure(ErrorCodes.UrlNotFound);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return urlTag.ExpirationDate <= now ?
            Result<string>.Failure(ErrorCodes.UrlExpired) : 
            Result<string>.Success(urlTag.DestinationURL);
    }

    private async Task<RedirectCacheItem?> GetFromDatabaseAsync(string shortCode,CancellationToken cancellationToken)
    {
        return await _context.UrlTags
            .AsNoTracking()
            .Where(x => x.ShortenedCode == shortCode)
            .Select(x => new RedirectCacheItem(
                x.DestinationURL,
                x.ExpirationDate))
            .FirstOrDefaultAsync(cancellationToken);
    }
}