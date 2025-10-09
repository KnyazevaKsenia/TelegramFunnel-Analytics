using Microsoft.EntityFrameworkCore;
using Telegram_Analytic.Infrastructure.Entities;

namespace Telegram_Analytic.Infrastructure.Database;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<Event> Events { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Link>()
            .HasIndex(l => l.RefCode)
            .IsUnique();

        modelBuilder.Entity<Event>()
            .HasIndex(e => e.RefCode);
    }
}