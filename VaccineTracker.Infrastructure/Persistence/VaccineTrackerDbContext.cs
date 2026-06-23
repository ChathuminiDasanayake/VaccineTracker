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
    public DbSet<Patient> Patients => Set<Patient>();

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

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.Property(patient => patient.PatientNumber)
                .HasMaxLength(50);

            entity.Property(patient => patient.FirstName)
                .HasMaxLength(100);

            entity.Property(patient => patient.LastName)
                .HasMaxLength(100);

            entity.Property(patient => patient.NationalIdNumber)
                .HasMaxLength(100);

            entity.Property(patient => patient.Email)
                .HasMaxLength(254);

            entity.Property(patient => patient.PhoneNumber)
                .HasMaxLength(30);

            entity.Property(patient => patient.StreetAddress)
                .HasMaxLength(300);

            entity.Property(patient => patient.City)
                .HasMaxLength(100);

            entity.Property(patient => patient.PostalCode)
                .HasMaxLength(20);

            entity.Property(patient => patient.Country)
                .HasMaxLength(100);

            entity.Property(patient => patient.EmergencyContactName)
                .HasMaxLength(200);

            entity.Property(patient => patient.EmergencyContactPhone)
                .HasMaxLength(30);

            entity.HasIndex(patient => patient.PatientNumber)
                .IsUnique();

            entity.HasOne(patient => patient.Hospital)
                .WithMany()
                .HasForeignKey(patient => patient.HospitalId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
