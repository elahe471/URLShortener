namespace Shortener.API.Infrastructure.Extensions
{
    public static class RedisDependencyInjection
    {
        public static void AddRedisCache(this IHostApplicationBuilder builder)
        {
            builder.Services.AddStackExchangeRedisCache(options => {
                    options.Configuration = builder.Configuration.GetConnectionString("Redis");
                    options.InstanceName = "Shortener:";});
        }
    }
}
