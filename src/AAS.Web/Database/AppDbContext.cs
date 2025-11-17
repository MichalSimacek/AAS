using AAS.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AAS.Web.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public DbSet<Collection> Collections => Set<Collection>();
        public DbSet<CollectionImage> CollectionImages => Set<CollectionImage>();
        public DbSet<CollectionTranslation> CollectionTranslations => Set<CollectionTranslation>();
        public DbSet<Inquiry> Inquiries => Set<Inquiry>();
        public DbSet<TranslationCache> TranslationCaches => Set<TranslationCache>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.HasPostgresExtension("uuid-ossp", "pgcrypto");

            b.Entity<Collection>(e =>
            {
                e.HasIndex(x => x.Slug).IsUnique();
                e.Property(x => x.Description).HasColumnType("text");
            });

            b.Entity<CollectionImage>(e =>
            {
                e.HasIndex(x => new { x.CollectionId, x.SortOrder });
            });

            b.Entity<CollectionTranslation>(e =>
            {
                e.HasIndex(x => new { x.CollectionId, x.LanguageCode }).IsUnique();
                e.Property(x => x.TranslatedDescription).HasColumnType("text");
            });

            b.Entity<TranslationCache>(e =>
            {
                e.HasIndex(x => x.SourceHash).IsUnique();
            });

            b.Entity<Comment>(e =>
            {
                e.HasIndex(x => x.CollectionId);
                e.HasIndex(x => x.CreatedAt);
            });

            b.Entity<BlogPost>(e =>
            {
                e.HasIndex(x => x.Published);
                e.HasIndex(x => x.CreatedAt);
                e.Property(x => x.ContentCs).HasColumnType("text");
                e.Property(x => x.ContentEn).HasColumnType("text");
                e.Property(x => x.ContentDe).HasColumnType("text");
                e.Property(x => x.ContentEs).HasColumnType("text");
                e.Property(x => x.ContentFr).HasColumnType("text");
                e.Property(x => x.ContentHi).HasColumnType("text");
                e.Property(x => x.ContentJa).HasColumnType("text");
                e.Property(x => x.ContentPt).HasColumnType("text");
                e.Property(x => x.ContentRu).HasColumnType("text");
                e.Property(x => x.ContentZh).HasColumnType("text");
            });
        }
    }
}