using Shortener.API.Infrastructure.Configurations;

namespace Shortener.API.Infrastructure.Extensions
{
    public static class ApplicationExtensions
    {
        public static void AddApplicationServices(this IHostApplicationBuilder builder)
        {
            builder.Services.AddDbContext<ShortenerURLContext>(options =>
        options.UseMongoDB(
            builder.Configuration.GetConnectionString("ShortenerURLContext") ??
            throw new InvalidOperationException(
                "Connection string 'ShortenerURLContext' not found.")));



            builder.Services
           .AddOptions<ShortenerSettings>()
           .Bind(builder.Configuration.GetSection(
               ShortenerSettings.SectionName))
           .Validate(
               x => x.ExpireDateScopeInDays > 0,
               "ExpireDateScopeInDays must be greater than zero.")
           .ValidateOnStart();



            builder.Services.AddScoped<IShortenService, ShortenService>();

        }
    }
}
