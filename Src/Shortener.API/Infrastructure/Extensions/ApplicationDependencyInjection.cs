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

            //instead of using DateTime.Now, we can use TimeProvider.System to get the current time, this will make it easier to test the code in the future
            builder.Services.AddSingleton(TimeProvider.System);

            builder.Services.AddScoped<IShortenService, ShortenService>();
            builder.Services.AddScoped<IRedirectService, RedirectService>();

        }
    }
}
