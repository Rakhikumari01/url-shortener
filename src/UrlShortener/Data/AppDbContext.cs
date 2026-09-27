using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;

namespace UrlShortener.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
        {
        }

        public DbSet<ShortUrl> ShortUrls { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var entity = modelBuilder.Entity<ShortUrl>();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(x => x.OriginalUrl).IsRequired().HasMaxLength(2048);
            entity.Property(x => x.Code).HasMaxLength(11);
            entity.HasIndex(x => x.Code).IsUnique();
        }
    }
}