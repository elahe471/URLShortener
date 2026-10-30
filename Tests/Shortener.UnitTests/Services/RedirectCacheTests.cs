using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shortener.API.Errors;
using Shortener.API.Infrastructure;
using Shortener.API.Infrastructure.Extensions;
using Shortener.API.Models;
using Shortener.API.Observability;
using Shortener.API.Services;
using Shortener.API.Services.DTOs;
using ZiggyCreatures.Caching.Fusion;

namespace Shortener.UnitTests.Services;

public sealed class RedirectCacheTests
{
    [Fact]
    public async Task RegisteredCache_PreservesDurationsAndSharesSerializedEntriesAcrossInstances()
    {
        var distributed = new TestDistributedCache();
        using var firstProvider = CreateProvider(distributed);
        using var secondProvider = CreateProvider(distributed);
        var first = firstProvider.GetRequiredService<IFusionCache>();
        var second = secondProvider.GetRequiredService<IFusionCache>();
        Assert.Equal(TimeSpan.FromMinutes(5), first.DefaultEntryOptions.Duration);
        Assert.Equal(TimeSpan.FromMinutes(30), first.DefaultEntryOptions.DistributedCacheDuration);
        Assert.False(first.DefaultEntryOptions.IsFailSafeEnabled);

        var item = new RedirectCacheItem("https://example.com", DateTime.UtcNow.AddHours(1));
        await first.SetAsync("r:Ab1234567", item);
        var resolved = await second.GetOrSetAsync<RedirectCacheItem?>("r:Ab1234567",
            (_, _) => throw new InvalidOperationException("The distributed cache should satisfy this request."));
        Assert.Equal(item, resolved);
    }

    [Fact]
    public async Task Resolve_CachesDestinationButStillRejectsExpiredLink()
    {
        using var provider = CreateProvider();
        var cache = provider.GetRequiredService<IFusionCache>();
        var clock = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await using var context = new ShortenerURLContext(new DbContextOptionsBuilder<ShortenerURLContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var link = new UrlTag
        {
            ShortenedCode = "Ab1234567", DestinationURL = "https://example.com",
            CreatedOn = clock.GetUtcNow().UtcDateTime,
            ExpirationDate = clock.GetUtcNow().AddMinutes(1).UtcDateTime
        };
        context.UrlTags.Add(link);
        await context.SaveChangesAsync();
        var service = new RedirectService(context, clock, cache,
            Options.Create(new Shortener.API.Infrastructure.Configurations.CacheSettings { UseCache = true }),
            NullLogger<RedirectService>.Instance, provider.GetRequiredService<ShortDiagnostic>());

        Assert.Equal(link.DestinationURL, (await service.ResolveAsync(link.ShortenedCode, default)).Value);
        context.UrlTags.Remove(link);
        await context.SaveChangesAsync();
        Assert.True((await service.ResolveAsync(link.ShortenedCode, default)).IsSuccess);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(ErrorCodes.UrlExpired, (await service.ResolveAsync(link.ShortenedCode, default)).ErrorCode);
    }

    private static ServiceProvider CreateProvider(IDistributedCache? distributed = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CacheSettings:UseCache"] = (distributed is not null).ToString(),
            ["CacheSettings:ExpirationInMinutes"] = "30",
            ["CacheSettings:LocalCacheExpirationInMinutes"] = "5",
            ["ConnectionStrings:Redis"] = "localhost:6379"
        });
        builder.AddCache();
        if (distributed is not null) builder.Services.AddSingleton(distributed);
        builder.Services.AddSingleton<ShortDiagnostic>();
        return builder.Services.BuildServiceProvider();
    }

    // Exercise the production registration without a Redis server. FusionCache
    // deliberately ignores the concrete MemoryDistributedCache by default.
    private sealed class TestDistributedCache : IDistributedCache
    {
        private readonly MemoryDistributedCache _inner = new(Options.Create(new MemoryDistributedCacheOptions()));
        public byte[]? Get(string key) => _inner.Get(key);
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => _inner.GetAsync(key, token);
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _inner.Set(key, value, options);
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => _inner.SetAsync(key, value, options, token);
        public void Refresh(string key) => _inner.Refresh(key);
        public Task RefreshAsync(string key, CancellationToken token = default) => _inner.RefreshAsync(key, token);
        public void Remove(string key) => _inner.Remove(key);
        public Task RemoveAsync(string key, CancellationToken token = default) => _inner.RemoveAsync(key, token);
    }
}
