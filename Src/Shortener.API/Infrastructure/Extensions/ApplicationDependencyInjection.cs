using Shortener.API.Infrastructure.Configurations;
using Shortener.API.Observability;

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
            x => !string.IsNullOrWhiteSpace(x.SecretKey),
            "SecretKey is required.")
        .Validate(
            x =>
            {
                try
                {
                    return Convert
                        .FromBase64String(x.SecretKey)
                        .Length >= 32;
                }
                catch
                {
                    return false;
                }
            },
            "SecretKey must be a valid Base64 value containing at least 32 bytes.")
        .ValidateOnStart();

           

            builder.Services.AddScoped<IShortenService, ShortenService>();
            builder.Services.AddScoped<IRedirectService, RedirectService>();

            //instead of using DateTime.Now, we can use TimeProvider.System to get the current time, this will make it easier to test the code in the future
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<ErrorTranslator>();
            builder.Services.AddSingleton<ShortDiagnostic>();

        }
    }
}
