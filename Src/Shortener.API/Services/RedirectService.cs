using System.Diagnostics;


namespace Shortener.API.Services;

public sealed class RedirectService(
    ShortenerURLContext context,
    TimeProvider timeProvider,
    HybridCache cache,
    IOptions<CacheSettings> cacheSettings,
    ILogger<RedirectService> logger,
    ShortDiagnostic shortDiagnostic)
    : IRedirectService
{
    private readonly ShortenerURLContext _context = context;
    private readonly TimeProvider _timeProvider =timeProvider;
    private readonly HybridCache _cache =cache;
    private readonly CacheSettings _cacheSettings =cacheSettings.Value;
    private readonly ILogger<RedirectService> _logger =logger;
    private readonly ShortDiagnostic _shortDiagnostic =shortDiagnostic;

    public async Task<Result<string>> ResolveAsync(string shortCode,CancellationToken cancellationToken)
    {
        var startedAt =Stopwatch.GetTimestamp();

        var redirectResult = "error";

        try
        {
            RedirectCacheItem? urlTag;

            if (_cacheSettings.UseCache)
            {
                var cacheKey =
                    $"r:{shortCode}";

                urlTag =
                    await _cache.GetOrCreateAsync(
                        cacheKey,
                        async cancel =>
                        {
                            // HybridCache could not resolve the value
                            // and must read it from the database.
                            _shortDiagnostic.CacheDatabaseFallback();

                            _logger.LogDebug("Cache miss for {ShortCode}. Reading from MongoDB.", shortCode);

                            return await GetFromDatabaseAsync(shortCode,cancel);
                        },
                        cancellationToken:
                            cancellationToken);
            }
            else
            {
                urlTag =await GetFromDatabaseAsync(shortCode,cancellationToken);
            }

            if (urlTag is null)
            {
                redirectResult = "not_found";

                return Result<string>.Failure(
                    ErrorCodes.UrlNotFound);
            }

            var now =
                _timeProvider
                    .GetUtcNow()
                    .UtcDateTime;

            if (urlTag.ExpirationDate <= now)
            {
                redirectResult = "expired";

                return Result<string>.Failure(
                    ErrorCodes.UrlExpired);
            }

            redirectResult = "success";

            return Result<string>.Success(
                urlTag.DestinationURL);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            redirectResult = "canceled";

            throw;
        }
        finally
        {
            var elapsed =Stopwatch.GetElapsedTime(startedAt);
            _shortDiagnostic.RedirectCompleted(redirectResult,elapsed.TotalSeconds);
        }
    }

    private async Task<RedirectCacheItem?> GetFromDatabaseAsync(string shortCode,CancellationToken cancellationToken)
    {
        return await _context.UrlTags
            .AsNoTracking()
            .Where(x =>
                x.ShortenedCode == shortCode)
            .Select(x =>
                new RedirectCacheItem(
                    x.DestinationURL,
                    x.ExpirationDate))
            .FirstOrDefaultAsync(
                cancellationToken);
    }
}