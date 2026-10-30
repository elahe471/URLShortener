public sealed class ErrorTranslator
{
    public IResult ToValidationResult(IDictionary<string, string[]> errors, HttpContext httpContext)
    {
        var problem = Translate(ErrorCodes.ValidationFailed, httpContext);
        return TypedResults.ValidationProblem(errors, detail: problem.Detail,
            title: problem.Title, type: problem.Type, extensions: problem.Extensions);
    }

    public ProblemDetails Translate(
        string errorCode,
        HttpContext httpContext,
        IDictionary<string, object?>? extensions = null)
    {
        var error = ErrorCatalog.Get(errorCode);

        var problemDetails = new ProblemDetails
        {
            Type = $"urn:shortener:error:{error.Code}",
            Title = error.Title,
            Detail = error.Message,
            Status = error.StatusCode
        };

        problemDetails.Extensions["code"] = error.Code;
        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions[extension.Key] =
                    extension.Value;
            }
        }

        return problemDetails;
    }

    public IResult ToResult(
        string errorCode,
        HttpContext httpContext,
        IDictionary<string, object?>? extensions = null)
    {
        var problemDetails =
            Translate(
                errorCode,
                httpContext,
                extensions);

        return Results.Problem(problemDetails);
    }
}
