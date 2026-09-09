using Microsoft.Extensions.Caching.Hybrid;

namespace Shortener.API.Infrastructure.Extensions
{
    public static class CacheDependencyInjection
    {
        public static void AddCache(this IHostApplicationBuilder builder)
        {
            var connectionString =
           builder.Configuration.GetConnectionString("Redis")
           ?? throw new InvalidOperationException(
               "Redis connection string is not configured.");



            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration =connectionString;
                options.InstanceName = "Shortener:";
            });
            var hybridCacheSettings =
    builder.Configuration
        .GetSection(CacheSettings.SectionName)
        .Get<CacheSettings>()
    ?? throw new InvalidOperationException(
        "HybridCache configuration is missing.");


            builder.Services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions =
                    new HybridCacheEntryOptions
                    {
                        Expiration = TimeSpan.FromMinutes(
                            hybridCacheSettings.ExpirationInMinutes),

                        LocalCacheExpiration = TimeSpan.FromMinutes(
                            hybridCacheSettings.LocalCacheExpirationInMinutes)
                    };
            });

            builder.Services.AddOptions<CacheSettings>().Bind(builder.Configuration.GetSection(CacheSettings.SectionName));

        }
    }
}
