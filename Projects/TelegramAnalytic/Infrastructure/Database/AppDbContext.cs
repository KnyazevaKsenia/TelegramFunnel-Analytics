using Microsoft.EntityFrameworkCore;
using Telegram_Analytic.Models;

namespace Telegram_Analytic.Infrastructure.Database;


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

        
        builder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("NOW()");

            
            entity.HasOne(p => p.User)
                .WithMany(u => u.Projects)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        
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

 
            entity.HasIndex(t => t.UrlIdentifier)
                .IsUnique();

            entity.HasOne(t => t.Project)
                .WithMany(p => p.TrackingLinks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
      
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


