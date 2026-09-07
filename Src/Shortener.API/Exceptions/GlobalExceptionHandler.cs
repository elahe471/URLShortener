namespace Shortener.API.Exceptions;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errorCode = ExceptionTranslator.Translate(exception);

        var error = ErrorCatalog.Get(errorCode);

        logger.LogError(
            exception,
            "Request failed with error code {ErrorCode}",
            error.Code);

        httpContext.Response.StatusCode = error.StatusCode;

        var problemDetails = new ProblemDetails
        {
            Type = $"urn:shortener:error:{error.Code}",
            Title = error.Title,
            Detail = error.Message,
            Status = error.StatusCode,

            Extensions =
            {
                ["code"] = error.Code,
                ["traceId"] = httpContext.TraceIdentifier
            }
        };

        if (!await problemDetailsService.TryWriteAsync(
                new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = problemDetails
                }))
        {
            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                cancellationToken);
        }

        return true;
    }
}