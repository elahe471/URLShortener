namespace Shortener.API.Infrastructure.Configurations;

public class CacheSettings
{
        public const string SectionName = "CacheSettings";

        public int ExpirationInMinutes { get; set; }

        public int LocalCacheExpirationInMinutes { get; set; }

    public bool UseCache { get; set; }

}
