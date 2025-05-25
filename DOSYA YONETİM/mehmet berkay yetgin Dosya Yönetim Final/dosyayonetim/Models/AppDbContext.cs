using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace dosyayonetim.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UserStorage> UserStorages { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<File> Files { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Folder>()
                .HasMany(f => f.Files)
                .WithOne(f => f.Folder)
                .HasForeignKey(f => f.FolderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure User-UserStorage relationship
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Storage)
                .WithOne(us => us.User)
                .HasForeignKey<UserStorage>(us => us.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure File-User relationship
            builder.Entity<File>()
                .HasOne(f => f.User)
                .WithMany(u => u.Files)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}