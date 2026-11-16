using TokenChat.Models;
using Microsoft.EntityFrameworkCore;

namespace TokenChat.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>().HasIndex(u => u.NormalizedUsername).IsUnique();
            modelBuilder.Entity<AppUser>().Property(u => u.Username).IsRequired();
            modelBuilder.Entity<AppUser>().Property(u => u.NormalizedUsername).IsRequired();
            modelBuilder.Entity<AppUser>().Property(u => u.PasswordHash).IsRequired();
        }
    }
}
