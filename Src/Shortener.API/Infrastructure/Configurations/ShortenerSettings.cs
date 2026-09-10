namespace Shortener.API.Infrastructure.Configurations
{
    public sealed class ShortenerSettings
    {
        public const string SectionName = "ShortenerSettings";


        public string SecretKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
    }
}
