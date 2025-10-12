using Microsoft.EntityFrameworkCore;
using Telegram_Analytic.Models;

namespace Telegram_Analytic.Infrastructure.Database;

// Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<Project> Projects { get; set; }
    public DbSet<TrackingLink> TrackingLinks { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Конфигурация Project
        builder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // Связь с пользователем
            entity.HasOne(p => p.User)
                .WithMany(u => u.Projects)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Конфигурация TrackingLink
        builder.Entity<TrackingLink>(entity =>
        {
            entity.HasKey(t => t.Id);
            
            entity.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(t => t.BaseUrl)
                .IsRequired();
                
            entity.Property(t => t.UrlIdentifier)
                .IsRequired();
                
            entity.Property(t => t.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // Индекс для быстрого поиска по идентификатору
            entity.HasIndex(t => t.UrlIdentifier)
                .IsUnique();

            // Связь с проектом
            entity.HasOne(t => t.Project)
                .WithMany(p => p.TrackingLinks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Конфигурация ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(u => u.Company)
                .HasMaxLength(100);
                
            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("NOW()");
        });
    }
}


