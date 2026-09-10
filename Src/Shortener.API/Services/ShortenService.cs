namespace Shortener.API.Services;

public sealed class ShortenService(
    ShortenerURLContext context,
    ISequenceGenerator sequenceGenerator,
    IShortCodeGenerator shortCodeGenerator,
    IOptions<ShortenerSettings> options,
    TimeProvider timeProvider)
    : IShortenService
{
    private readonly ShortenerURLContext _context = context;
    private readonly ISequenceGenerator _sequenceGenerator = sequenceGenerator;
    private readonly IShortCodeGenerator _shortCodeGenerator = shortCodeGenerator;
    private readonly ShortenerSettings _settings = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<string> ShortenUrlAsync(
      string longUrl,
      DateTimeOffset expirationDate,
      CancellationToken cancellationToken)
    {
        var sequence =
            await _sequenceGenerator.GetNextAsync(
                cancellationToken);

        var shortenedCode =
            _shortCodeGenerator.Generate(sequence);

        var now =
            _timeProvider.GetUtcNow().UtcDateTime;

        var urlTag = new UrlTag
        {
            ShortenedCode = shortenedCode,
            DestinationURL = longUrl,
            CreatedOn = now,
            ExpirationDate = expirationDate.UtcDateTime
        };

        _context.UrlTags.Add(urlTag);

        await _context.SaveChangesAsync(
            cancellationToken);

        return GetShortenedUrl(shortenedCode);
    }

    private string GetShortenedUrl(
        string shortenedCode)
    {
        return $"{_settings.BaseUrl.TrimEnd('/')}/{shortenedCode}";
    }
}