

using SharpCompress.Common;

namespace Shortener.API.Infrastructure
{
    public class ShortenerURLContext : DbContext
    {
        public ShortenerURLContext(DbContextOptions<ShortenerURLContext> options) : base(options)
        {

        }
        public DbSet<UrlTag> UrlTags { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UrlTag>().ToCollection("UrlTags");

            modelBuilder.Entity<UrlTag>(entity =>
            {
                entity.HasIndex(x => x.ShortenedCode)
                    .IsUnique();

                entity.HasIndex(x => x.DestinationURL);

                entity.HasIndex(x => x.ExpirationDate);

                
            });

        }
    }
}
