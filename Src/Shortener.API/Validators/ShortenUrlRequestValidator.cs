using FluentValidation;
using Shortener.API.Endpoints.Contracts;

namespace Shortener.API.Validators;

public sealed class ShortenUrlRequestValidator : AbstractValidator<ShortenUrlRequest>
{
    private const int MaxUrlLength = 4096;

    public ShortenUrlRequestValidator()
    {
        RuleFor(x => x.LongURL)
            .NotEmpty()
            .WithMessage("URL is required.")

            .MaximumLength(MaxUrlLength)
            .WithMessage($"URL cannot exceed {MaxUrlLength} characters.")

            .Must(BeValidUrl)
            .WithMessage("URL must be a valid absolute HTTP or HTTPS URL.");
    }

    private static bool BeValidUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttp ||
               uri.Scheme == Uri.UriSchemeHttps;
    }
}