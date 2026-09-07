namespace Shortener.API.Errors.CustomModel
{
    public sealed record ErrorDefinition(
     string Code,
     string Title,
     string Message,
     int StatusCode);
}
