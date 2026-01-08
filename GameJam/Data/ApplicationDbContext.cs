// Data/ApplicationDbContext.cs
using GameJam.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace SBUGameJam.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Team configuration
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasIndex(e => e.TeamName);
            entity.HasIndex(e => e.SanitizedFolderName).IsUnique();
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });

        // TeamMember configuration
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasIndex(e => e.PhoneNumber);
            entity.HasIndex(e => new { e.TeamId, e.DisplayOrder });

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}