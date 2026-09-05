using Microsoft.Extensions.Options;
using Shortener.API.Exceptions;
using Shortener.API.Infrastructure.Configurations;
using System.Security.Cryptography;

namespace Shortener.API.Services;

public class ShortenService(
    ShortenerURLContext context,
    ILogger<ShortenService> logger,
       IOptions<ShortenerSettings> settings)
    : IShortenService
{
    private readonly ShortenerURLContext _context = context;
    private readonly ILogger<ShortenService> _logger = logger;
    private readonly IOptions<ShortenerSettings>  _settings = settings;

    private const string Alphabet =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private const int ShortCodeLength = 10;

    public async Task<string> ShortenUrlAsync(
        string longUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingUrlTag = await _context.UrlTags
                .FirstOrDefaultAsync(
                    u => u.DestinationURL == longUrl,
                    cancellationToken);

            if (existingUrlTag is not null)
            {
                return existingUrlTag.ShortenedCode;
            }

            var shortenedCode =
                await GenerateUniqueShortenedCodeAsync(cancellationToken);

            var now = DateTime.UtcNow;

            var urlTag = new UrlTag
            {
                ShortenedCode = shortenedCode,
                DestinationURL = longUrl,
                CreatedOn = now,
                ExpirationDate = now.AddDays(
                    _settings.Value.ExpireDateScopeInDays)
            };

            await _context.UrlTags.AddAsync(
                urlTag,
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return shortenedCode;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ShortCodeGenerationException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database update failed while shortening URL.");

            throw new DatabaseOperationException(
                "Unable to save the shortened URL.",
                ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(
                ex,
                "Database operation timed out while shortening URL.");

            throw new DatabaseOperationException(
                "The database operation timed out.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while shortening URL.");

            throw;
        }
    }

    private async Task<string> GenerateUniqueShortenedCodeAsync(
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var code = GenerateShortenedCode();

            var exists = await _context.UrlTags
                .AnyAsync(
                    x => x.ShortenedCode == code,
                    cancellationToken);

            if (!exists)
            {
                return code;
            }
        }

        throw new ShortCodeGenerationException();
    }

    private static string GenerateShortenedCode()
    {
        Span<char> code =
            stackalloc char[ShortCodeLength];

        for (var i = 0; i < code.Length; i++)
        {
            code[i] = Alphabet[
                RandomNumberGenerator.GetInt32(
                    Alphabet.Length)];
        }

        return new string(code);
    }
}