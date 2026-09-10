

public sealed class ShortenUrlRequestValidator
    : AbstractValidator<ShortenUrlRequest>
{
    private const int MaxUrlLength = 4096;

    public ShortenUrlRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.LongUrl)
            .NotEmpty()
            .WithMessage("URL is required.")

            .MaximumLength(MaxUrlLength)
            .WithMessage($"URL cannot exceed {MaxUrlLength} characters.")

            .Must(BeValidUrl)
            .WithMessage("URL must be a valid absolute HTTP or HTTPS URL.");

        RuleFor(x => x.ExpirationDate)
           .Must(expirationDate =>
               expirationDate > timeProvider.GetUtcNow())
           .WithMessage(
               "ExpirationDate must be in the future.");
    }

    private static bool BeValidUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
            return false;

        if (string.IsNullOrWhiteSpace(uri.Host))
            return false;

        if (url.Any(char.IsControl))
            return false;

        return true;
    }
}