namespace Shortener.API.Infrastructure.Configurations
{
    public sealed class ShortenerSettings
    {
        public const string SectionName = "ShortenerSettings";

        public int ExpireDateScopeInDays { get; set; }
    }
}
