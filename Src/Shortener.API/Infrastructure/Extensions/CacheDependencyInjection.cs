using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Shortener.API.Infrastructure.Extensions;

public static class CacheDependencyInjection
{
    public static void AddCache(this IHostApplicationBuilder builder)
    {
        var cacheSettings = builder.Configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>()
            ?? throw new InvalidOperationException("Cache configuration is missing.");

        var fusionCache = builder.Services.AddFusionCache()
            .WithDefaultEntryOptions(options =>
            {
                options.Duration = TimeSpan.FromMinutes(cacheSettings.LocalCacheExpirationInMinutes);
                options.DistributedCacheDuration = TimeSpan.FromMinutes(cacheSettings.ExpirationInMinutes);
                options.IsFailSafeEnabled = false;
                // Keep factory work inside the request scope that owns the DbContext.
                options.AllowTimedOutFactoryBackgroundCompletion = false;
                options.AllowBackgroundDistributedCacheOperations = false;
            });

        if (cacheSettings.UseCache)
        {
            var connectionString = builder.Configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Redis connection string is not configured.");

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = "Shortener:";
            });

            fusionCache.WithSerializer(new FusionCacheSystemTextJsonSerializer())
                .WithRegisteredDistributedCache();
        }

        builder.Services.AddOptions<CacheSettings>()
            .Bind(builder.Configuration.GetSection(CacheSettings.SectionName));
    }
}