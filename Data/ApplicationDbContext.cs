using Microsoft.EntityFrameworkCore;
using VSSAuthPrototype.Models;

namespace VSSAuthPrototype.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Stream> Streams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Users table
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("AbpUsers");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserName).IsUnique();
            });

            // Configure Streams table
            modelBuilder.Entity<Stream>(entity =>
            {
                entity.ToTable("Streams");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Slug).IsUnique();
            });
        }
    }
}
