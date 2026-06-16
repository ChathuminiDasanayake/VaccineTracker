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
    }
}
