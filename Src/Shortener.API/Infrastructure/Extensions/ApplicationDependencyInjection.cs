using Shortener.API.Infrastructure.Configurations;

namespace Shortener.API.Infrastructure.Extensions
{
    public static class ApplicationExtensions
    {
        public static void AddApplicationServices(this IHostApplicationBuilder builder)
        {

            builder.Services
         .AddOptions<ShortenerSettings>()
         .Bind(builder.Configuration.GetSection(
             ShortenerSettings.SectionName))
         .Validate(
             x => x.ExpireDateScopeInDays > 0,
             "ExpireDateScopeInDays must be greater than zero.")
         .Validate(
             x => !string.IsNullOrWhiteSpace(x.SecretKey),
             "SecretKey is required.")
         .Validate(
             x => Encoding.UTF8.GetByteCount(x.SecretKey) >= 32,
             "SecretKey must be at least 32 bytes.")
         .ValidateOnStart();


            builder.Services.AddScoped<IShortenService, ShortenService>();

        }
    }
}
