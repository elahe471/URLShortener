
namespace Shortener.API.Services;

public class ShortenService(
    ShortenerURLContext context,
    ISequenceGenerator sequenceGenerator,
    IShortCodeGenerator shortCodeGenerator,
    IOptions<ShortenerSettings> options)
    : IShortenService
{
    private readonly ShortenerURLContext _context = context;
    private readonly IOptions<ShortenerSettings> _settings = options        ;


    public async Task<string> ShortenUrlAsync(
        string longUrl,
        CancellationToken cancellationToken)
    {
        var sequence =
         await sequenceGenerator.GetNextAsync(
             cancellationToken);

        var shortenedCode =
            shortCodeGenerator.Generate(sequence);

        var now = DateTime.UtcNow;

        var urlTag = new UrlTag
        {
            ShortenedCode = shortenedCode,
            DestinationURL = longUrl,
            CreatedOn = now,
            ExpirationDate = now.AddDays(
                _settings.Value.ExpireDateScopeInDays)
        };

        _context.UrlTags.Add(urlTag);

        await _context.SaveChangesAsync(
            cancellationToken);

        return shortenedCode;
    }

}