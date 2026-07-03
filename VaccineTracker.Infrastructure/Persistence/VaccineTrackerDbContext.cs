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
    public DbSet<VaccineType> VaccineTypes => Set<VaccineType>();
    public DbSet<VaccineProduct> VaccineProducts => Set<VaccineProduct>();
    public DbSet<VaccineScheduleItem> VaccineScheduleItems => Set<VaccineScheduleItem>();
    public DbSet<VaccinationRecord> VaccinationRecords => Set<VaccinationRecord>();
    public DbSet<VaccineManufacturer> VaccineManufacturers => Set<VaccineManufacturer>();
    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();

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

        modelBuilder.Entity<VaccineType>(entity =>
        {
            entity.HasIndex(vaccineType => vaccineType.Code)
                .IsUnique();

            entity.Property(vaccineType => vaccineType.Code)
                .HasMaxLength(50);

            entity.Property(vaccineType => vaccineType.Name)
                .HasMaxLength(200);

            entity.Property(vaccineType => vaccineType.DiseaseTarget)
                .HasMaxLength(200);

            entity.Property(vaccineType => vaccineType.Description)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<VaccineProduct>(entity =>
        {
            entity.HasIndex(product => product.Code)
                .IsUnique();

            entity.Property(product => product.Code)
                .HasMaxLength(50);

            entity.Property(product => product.Name)
                .HasMaxLength(200);

            entity.Property(product => product.Description)
                .HasMaxLength(500);

            entity.HasOne(product => product.VaccineType)
                .WithMany(vaccineType => vaccineType.Products)
                .HasForeignKey(product => product.VaccineTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(product => product.Manufacturer)
                .WithMany(manufacturer => manufacturer.VaccineProducts)
                .HasForeignKey(product => product.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VaccineScheduleItem>(entity =>
        {
            entity.HasIndex(x => new { x.VaccineTypeId, x.TargetGroup, x.DoseNumber })
                .IsUnique();

            entity.Property(x => x.Description).HasMaxLength(500);

            entity.HasOne(scheduleItem => scheduleItem.VaccineType)
                .WithMany(vaccineType => vaccineType.ScheduleItems)
                .HasForeignKey(scheduleItem => scheduleItem.VaccineTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VaccinationRecord>(entity =>
        {
            entity.Property(x => x.BatchNumber).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.HospitalId);
            entity.HasIndex(x => x.VaccineProductId);
            entity.HasIndex(x => x.AdministeredDate);

            entity.HasOne(record => record.Patient)
                .WithMany()
                .HasForeignKey(record => record.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(record => record.Hospital)
                .WithMany()
                .HasForeignKey(record => record.HospitalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(record => record.VaccineProduct)
                .WithMany(product => product.VaccinationRecords)
                .HasForeignKey(record => record.VaccineProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(record => record.VaccineScheduleItem)
                .WithMany()
                .HasForeignKey(record => record.VaccineScheduleItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VaccineManufacturer>(entity =>
        {
            entity.HasIndex(manufacturer => manufacturer.Code)
                .IsUnique();

            entity.Property(manufacturer => manufacturer.Code)
                .HasMaxLength(50);

            entity.Property(manufacturer => manufacturer.Name)
                .HasMaxLength(200);

            entity.Property(manufacturer => manufacturer.Description)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<NotificationOutbox>(entity =>
        {
            entity.Property(notification => notification.Recipient)
                .HasMaxLength(254);

            entity.Property(notification => notification.Subject)
                .HasMaxLength(300);

            entity.Property(notification => notification.PayloadJson)
                .HasMaxLength(4000);

            entity.Property(notification => notification.FailureReason)
                .HasMaxLength(1000);

            entity.HasIndex(notification => new
            {
                notification.Status,
                notification.SendAfterUtc
            });

            entity.HasIndex(notification => notification.PatientId);

            entity.HasIndex(notification => notification.VaccineScheduleItemId);

            entity.HasIndex(notification => notification.VaccinationRecordId);

            entity.HasOne(notification => notification.Patient)
                .WithMany()
                .HasForeignKey(notification => notification.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(notification => notification.VaccineScheduleItem)
                .WithMany()
                .HasForeignKey(notification => notification.VaccineScheduleItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(notification => notification.VaccinationRecord)
                .WithMany()
                .HasForeignKey(notification => notification.VaccinationRecordId)
                .OnDelete(DeleteBehavior.Restrict);
        });


    }
}
