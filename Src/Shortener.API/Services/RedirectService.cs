using Shortener.API.Errors.CustomModel;

namespace Shortener.API.Services;

public sealed class RedirectService(
    ShortenerURLContext context,
    TimeProvider timeProvider)
    : IRedirectService
{
    private readonly ShortenerURLContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<string>> ResolveAsync(
        string shortCode,
        CancellationToken cancellationToken)
    {
        var urlTag = await _context.UrlTags
            .AsNoTracking()
            .Where(x => x.ShortenedCode == shortCode)
            .Select(x => new
            {
                x.DestinationURL,
                x.ExpirationDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (urlTag is null)
        {
            return Result<string>.Failure(
                ErrorCodes.UrlNotFound);
        }

        var now = _timeProvider
            .GetUtcNow()
            .UtcDateTime;

        if (urlTag.ExpirationDate <= now)
        {
            return Result<string>.Failure(
                ErrorCodes.UrlExpired);
        }

        return Result<string>.Success(
            urlTag.DestinationURL);
    }
}