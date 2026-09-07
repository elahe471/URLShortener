using Shortener.API.Errors.CustomModel;

namespace Shortener.API.Errors;



public static class ErrorCatalog
{
    private static readonly FrozenDictionary<string, ErrorDefinition> Errors =
        new Dictionary<string, ErrorDefinition>
        {
            [ErrorCodes.InvalidShortCode] = new(
                ErrorCodes.InvalidShortCode,
                "Invalid short code",
                "The provided short code is invalid.",
                StatusCodes.Status400BadRequest),

            [ErrorCodes.UrlNotFound] = new(
                ErrorCodes.UrlNotFound,
                "Short URL not found",
                "The requested short URL does not exist.",
                StatusCodes.Status404NotFound),

            [ErrorCodes.UrlExpired] = new(
                ErrorCodes.UrlExpired,
                "Short URL expired",
                "The requested short URL has expired.",
                StatusCodes.Status410Gone),

            [ErrorCodes.DuplicateShortCode] = new(
                ErrorCodes.DuplicateShortCode,
                "Short code conflict",
                "The generated short code already exists.",
                StatusCodes.Status409Conflict),

            [ErrorCodes.DatabaseUnavailable] = new(
                ErrorCodes.DatabaseUnavailable,
                "Service unavailable",
                "The database is temporarily unavailable.",
                StatusCodes.Status503ServiceUnavailable),

            [ErrorCodes.Unexpected] = new(
                ErrorCodes.Unexpected,
                "Internal server error",
                "An unexpected error occurred.",
                StatusCodes.Status500InternalServerError),

            [ErrorCodes.ValidationFailed] = new(
    ErrorCodes.ValidationFailed,
    "Validation failed",
    "One or more validation errors occurred.",
    StatusCodes.Status400BadRequest),

    
        }
        .ToFrozenDictionary();

    public static ErrorDefinition Get(string code)
    {
        return Errors.TryGetValue(code, out var error)
            ? error
            : Errors[ErrorCodes.Unexpected];
    }
}
