// VaccineTracker.Infrastructure/Persistence/VaccineTrackerDbContext.cs
using Microsoft.EntityFrameworkCore;
using VaccineTracker.Domain.Entities;

namespace VaccineTracker.Infrastructure.Persistence;

public sealed class VaccineTrackerDbContext : DbContext
{
    public VaccineTrackerDbContext(DbContextOptions<VaccineTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Hospital> Hospitals => Set<Hospital>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<LoginAudit> LoginAudits => Set<LoginAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VaccineTrackerDbContext).Assembly);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(user => user.NormalizedUsername)
                .HasMaxLength(100);

            entity.Property(user => user.NormalizedEmail)
                .HasMaxLength(254);

            entity.HasIndex(user => user.NormalizedUsername)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<LoginAudit>(entity =>
        {
            entity.Property(x => x.Username).HasMaxLength(100);
            entity.Property(x => x.FailureReason).HasMaxLength(200);
            entity.Property(x => x.IpAddress).HasMaxLength(45);
            entity.Property(x => x.UserAgent).HasMaxLength(500);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.AttemptedAtUtc);
            entity.HasIndex(x => x.CorrelationId);
        });
    }
}
