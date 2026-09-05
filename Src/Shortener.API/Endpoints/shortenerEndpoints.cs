using FluentValidation;
using Shortener.API.Endpoints.Contracts;

namespace Shortener.API.Endpoints
{
    public static class ShortenerEndpoints
    {
        public static IEndpointRouteBuilder MapShortenerEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/", CreateShortUrl);

            return app;
        }

        public static async Task<IResult> CreateShortUrl(ShortenUrlRequest request, IValidator<ShortenUrlRequest> validator,IShortenService shortenService,
        CancellationToken cancellationToken)
        {
            //URL Validation
            var validationResult =
                await validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(
                    validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Select(e => e.ErrorMessage).ToArray()));
            }

            var shortUrl = await shortenService.ShortenUrlAsync(request.LongUrl, cancellationToken);

            return Results.Ok(shortUrl);
        }
    }
}
