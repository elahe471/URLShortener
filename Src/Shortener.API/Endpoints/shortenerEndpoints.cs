

namespace Shortener.API.Endpoints;

public static class ShortenerEndpoints
{
    public static IEndpointRouteBuilder MapShortenerEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/", CreateShortUrl);

        return app;
    }

    public static async Task<IResult> CreateShortUrl(
        ShortenUrlRequest request,
        IValidator<ShortenUrlRequest> validator,
        IShortenService shortenService,
        ErrorTranslator errorTranslator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await validator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(e => e.ErrorMessage).ToArray());

            return errorTranslator.ToResult(
                ErrorCodes.ValidationFailed,
                httpContext,
                new Dictionary<string, object?>
                {
                    ["errors"] = errors
                });
        }

        var shortUrl =
            await shortenService.ShortenUrlAsync(
                request.LongUrl,
                cancellationToken);

        return Results.Ok(shortUrl);
    }
}