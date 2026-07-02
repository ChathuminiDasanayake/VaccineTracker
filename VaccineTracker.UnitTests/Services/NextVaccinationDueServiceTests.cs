using Microsoft.EntityFrameworkCore;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class NextVaccinationDueServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Test]
    public async Task GetNextDueForPatientAsync_ReturnsEarliestUncompletedScheduleItem()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedChildScheduleAsync(dbContext);
        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.GetNextDueForPatientAsync(seed.Patient.Id);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.PatientId, Is.EqualTo(seed.Patient.Id));
            Assert.That(result.VaccineScheduleItemId, Is.EqualTo(seed.Dose1.Id));
            Assert.That(result.DoseNumber, Is.EqualTo(1));
            Assert.That(result.DueDate, Is.EqualTo(seed.Patient.DateOfBirth.AddDays(270)));
            Assert.That(result.TargetGroup, Is.EqualTo("Child"));
        });
    }

    [Test]
    public async Task GetNextDueForPatientAsync_WhenFirstDoseCompleted_ReturnsSecondDose()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedChildScheduleAsync(dbContext);
        await AddRecordAsync(dbContext, seed, seed.Dose1, doseNumber: 1);
        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.GetNextDueForPatientAsync(seed.Patient.Id);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.VaccineScheduleItemId, Is.EqualTo(seed.Dose2.Id));
            Assert.That(result.DoseNumber, Is.EqualTo(2));
            Assert.That(result.DueDate, Is.EqualTo(seed.Patient.DateOfBirth.AddDays(450)));
        });
    }

    [Test]
    public async Task GetNextDueAfterRecordAsync_ReturnsNextDoseForSameVaccineType()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedChildScheduleAsync(dbContext);
        var record = await AddRecordAsync(dbContext, seed, seed.Dose1, doseNumber: 1);
        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.GetNextDueAfterRecordAsync(record.Id);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.VaccineScheduleItemId, Is.EqualTo(seed.Dose2.Id));
            Assert.That(result.DoseNumber, Is.EqualTo(2));
            Assert.That(result.VaccineTypeId, Is.EqualTo(seed.VaccineType.Id));
        });
    }

    [Test]
    public async Task GetNextDueAfterRecordAsync_WhenNoNextDose_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedChildScheduleAsync(dbContext);
        var record = await AddRecordAsync(dbContext, seed, seed.Dose2, doseNumber: 2);
        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.GetNextDueAfterRecordAsync(record.Id);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetNextDueAfterRecordAsync_WithRepeatInterval_UsesAdministeredDate()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedChildScheduleAsync(dbContext);
        var repeatDose = new VaccineScheduleItem
        {
            VaccineTypeId = seed.VaccineType.Id,
            TargetGroup = VaccineTargetGroup.Child,
            RepeatIntervalInDays = 365,
            DoseNumber = 2,
            Status = EntityStatus.Active
        };
        seed.Dose2.Status = EntityStatus.Inactive;
        dbContext.VaccineScheduleItems.Add(repeatDose);
        await dbContext.SaveChangesAsync();

        var record = await AddRecordAsync(
            dbContext,
            seed,
            seed.Dose1,
            doseNumber: 1,
            administeredDate: new DateOnly(2026, 1, 10));

        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.GetNextDueAfterRecordAsync(record.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.DueDate, Is.EqualTo(new DateOnly(2027, 1, 10)));
    }

    [Test]
    public async Task GetNextDueForPatientAsync_WhenAllDosesCompleted_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedChildScheduleAsync(dbContext);
        await AddRecordAsync(dbContext, seed, seed.Dose1, doseNumber: 1);
        await AddRecordAsync(dbContext, seed, seed.Dose2, doseNumber: 2);
        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.GetNextDueForPatientAsync(seed.Patient.Id);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetNextDueForPatientAsync_WhenPatientBelongsToOtherHospital_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedChildScheduleAsync(dbContext);
        var service = CreateService(dbContext, Guid.NewGuid());

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.GetNextDueForPatientAsync(seed.Patient.Id));
    }

    [Test]
    public async Task GetNextDueForPatientAsync_WhenNoHospitalClaim_ThrowsForbiddenException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedChildScheduleAsync(dbContext);
        var service = CreateService(dbContext, hospitalId: null);

        Assert.ThrowsAsync<ForbiddenException>(async () =>
            await service.GetNextDueForPatientAsync(seed.Patient.Id));
    }

    private static NextVaccinationDueService CreateService(
        VaccineTrackerDbContext dbContext,
        Guid? hospitalId)
    {
        return new NextVaccinationDueService(
            dbContext,
            new TestCurrentUser(CurrentUserId, hospitalId));
    }

    private static async Task<DueSeed> SeedChildScheduleAsync(
        VaccineTrackerDbContext dbContext)
    {
        var hospital = new Hospital
        {
            Name = "Children Hospital",
            IsActive = true
        };

        var patient = new Patient
        {
            HospitalId = hospital.Id,
            PatientNumber = "PT-0001",
            FirstName = "Child",
            LastName = "Patient",
            DateOfBirth = new DateOnly(2025, 1, 1),
            Gender = Gender.Female,
            Status = EntityStatus.Active
        };

        var vaccineType = new VaccineType
        {
            Name = "MMR Vaccine",
            Code = "MMR",
            DiseaseTarget = "Measles Mumps Rubella",
            Status = EntityStatus.Active
        };

        var manufacturer = new VaccineManufacturer
        {
            Name = "GSK",
            Code = "GSK",
            Status = EntityStatus.Active
        };

        dbContext.AddRange(hospital, patient, vaccineType, manufacturer);
        await dbContext.SaveChangesAsync();

        var product = new VaccineProduct
        {
            VaccineTypeId = vaccineType.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Priorix",
            Code = "PRIORIX",
            Status = EntityStatus.Active
        };

        var dose1 = new VaccineScheduleItem
        {
            VaccineTypeId = vaccineType.Id,
            TargetGroup = VaccineTargetGroup.Child,
            DueAgeInDays = 270,
            DoseNumber = 1,
            Description = "MMR dose 1",
            Status = EntityStatus.Active
        };

        var dose2 = new VaccineScheduleItem
        {
            VaccineTypeId = vaccineType.Id,
            TargetGroup = VaccineTargetGroup.Child,
            DueAgeInDays = 450,
            DoseNumber = 2,
            Description = "MMR dose 2",
            Status = EntityStatus.Active
        };

        dbContext.AddRange(product, dose1, dose2);
        await dbContext.SaveChangesAsync();

        return new DueSeed(
            hospital,
            patient,
            vaccineType,
            manufacturer,
            product,
            dose1,
            dose2);
    }

    private static async Task<VaccinationRecord> AddRecordAsync(
        VaccineTrackerDbContext dbContext,
        DueSeed seed,
        VaccineScheduleItem scheduleItem,
        int doseNumber,
        DateOnly? administeredDate = null)
    {
        var record = new VaccinationRecord
        {
            PatientId = seed.Patient.Id,
            HospitalId = seed.Hospital.Id,
            VaccineProductId = seed.Product.Id,
            VaccineScheduleItemId = scheduleItem.Id,
            DoseNumber = doseNumber,
            AdministeredDate = administeredDate ?? new DateOnly(2026, 1, 1),
            AdministeredByUserId = CurrentUserId,
            Status = VaccinationRecordStatus.Administered
        };

        dbContext.VaccinationRecords.Add(record);
        await dbContext.SaveChangesAsync();

        return record;
    }

    private static VaccineTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VaccineTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaccineTrackerDbContext(options);
    }

    private sealed record DueSeed(
        Hospital Hospital,
        Patient Patient,
        VaccineType VaccineType,
        VaccineManufacturer Manufacturer,
        VaccineProduct Product,
        VaccineScheduleItem Dose1,
        VaccineScheduleItem Dose2);

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(Guid userId, Guid? hospitalId)
        {
            UserId = userId;
            HospitalId = hospitalId;
        }

        public Guid UserId { get; }

        public Guid? HospitalId { get; }

        public string Email => "doctor@test.com";

        public string Role => Domain.Enums.Role.Doctor.ToString();
    }
}
