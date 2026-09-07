namespace Shortener.API.Services.Interface;

public interface ISequenceGenerator
{
    Task<long> GetNextAsync(
        CancellationToken cancellationToken);
}
