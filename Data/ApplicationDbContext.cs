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
        public DbSet<VssStream> Streams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Users table (PostgreSQL uses lowercase by default)
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserName).IsUnique();
            });

            // Configure Streams table
            modelBuilder.Entity<VssStream>(entity =>
            {
                entity.ToTable("streams");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Slug).IsUnique();
            });
        }
    }
}