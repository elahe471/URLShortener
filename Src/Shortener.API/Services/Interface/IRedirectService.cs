using Shortener.API.Errors.CustomModel;

namespace Shortener.API.Services.Interface;

public interface IRedirectService
{
    Task<Result<string>> ResolveAsync(
     string shortCode,
     CancellationToken cancellationToken);
}
