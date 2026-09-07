
namespace Shortener.API.Endpoints;

public static class RedirectEndpoints
{
    public static IEndpointRouteBuilder MapRedirectEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/{shortCode:length(9)}", RedirectToLongUrl);

        return app;
    }

    public static async Task<IResult> RedirectToLongUrl(
        [FromRoute] string shortCode,
        IRedirectService redirectService,
        ErrorTranslator errorTranslator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!IsValidShortCode(shortCode))
        {
            return errorTranslator.ToResult(
                ErrorCodes.InvalidShortCode,
                httpContext);
        }

        var result = await redirectService.ResolveAsync(
            shortCode,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return errorTranslator.ToResult(
                result.ErrorCode!,
                httpContext);
        }

        return Results.Redirect(
            result.Value!,
            permanent: false);
    }

    private static bool IsValidShortCode(string shortCode)
    {
        return shortCode.Length == 9 &&
               shortCode.All(char.IsLetterOrDigit);
    }
}