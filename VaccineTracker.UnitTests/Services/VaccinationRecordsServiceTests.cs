using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.VaccinationRecords;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class VaccinationRecordsServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Test]
    public async Task CreateRecordAsync_WithValidRequest_CreatesAdministeredRecord()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.CreateRecordAsync(
            CreateRequest(
                seed.Patient.Id,
                seed.Product.Id,
                seed.ScheduleItem.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.PatientId, Is.EqualTo(seed.Patient.Id));
            Assert.That(result.HospitalId, Is.EqualTo(seed.Hospital.Id));
            Assert.That(result.VaccineProductCode, Is.EqualTo("PRIORIX"));
            Assert.That(result.VaccineTypeCode, Is.EqualTo(seed.VaccineType.Code));
            Assert.That(result.Status, Is.EqualTo("Administered"));
            Assert.That(result.AdministeredByUserId, Is.EqualTo(CurrentUserId));
        });

        var record = await dbContext.VaccinationRecords.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(record.CreatedBy, Is.EqualTo(CurrentUserId.ToString()));
            Assert.That(record.AdministeredByUserId, Is.EqualTo(CurrentUserId));
        });
    }

    [Test]
    public async Task CreateRecordAsync_WhenPatientBelongsToOtherHospital_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var otherHospitalId = Guid.NewGuid();
        await AddHospitalAsync(dbContext, otherHospitalId);
        var service = CreateService(dbContext, otherHospitalId);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.CreateRecordAsync(
                CreateRequest(
                    seed.Patient.Id,
                    seed.Product.Id,
                    seed.ScheduleItem.Id)));
    }

    [Test]
    public async Task CreateRecordAsync_WhenCurrentUserHasNoHospital_ThrowsForbiddenException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var service = CreateService(dbContext, hospitalId: null);

        Assert.ThrowsAsync<ForbiddenException>(async () =>
            await service.CreateRecordAsync(
                CreateRequest(
                    seed.Patient.Id,
                    seed.Product.Id,
                    seed.ScheduleItem.Id)));
    }

    [Test]
    public async Task CreateRecordAsync_WhenAdministeredDateFuture_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var service = CreateService(dbContext, seed.Hospital.Id);

        Assert.ThrowsAsync<ValidationException>(async () =>
            await service.CreateRecordAsync(
                CreateRequest(
                    seed.Patient.Id,
                    seed.Product.Id,
                    seed.ScheduleItem.Id,
                    administeredDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))));
    }

    [Test]
    public async Task CreateRecordAsync_WhenScheduleItemDoesNotMatchProduct_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var otherVaccineType = new VaccineType
        {
            Name = "BCG Vaccine",
            Code = "BCG",
            DiseaseTarget = "Tuberculosis",
            Status = EntityStatus.Active
        };
        dbContext.VaccineTypes.Add(otherVaccineType);
        await dbContext.SaveChangesAsync();

        var otherScheduleItem = new VaccineScheduleItem
        {
            VaccineTypeId = otherVaccineType.Id,
            TargetGroup = VaccineTargetGroup.Child,
            DueAgeInDays = 1,
            DoseNumber = 1,
            Status = EntityStatus.Active
        };
        dbContext.VaccineScheduleItems.Add(otherScheduleItem);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, seed.Hospital.Id);

        Assert.ThrowsAsync<ValidationException>(async () =>
            await service.CreateRecordAsync(
                CreateRequest(
                    seed.Patient.Id,
                    seed.Product.Id,
                    otherScheduleItem.Id)));
    }

    [Test]
    public async Task CreateRecordAsync_WhenDuplicateScheduleDose_ThrowsConflictException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        await AddVaccinationRecordAsync(dbContext, seed);
        var service = CreateService(dbContext, seed.Hospital.Id);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await service.CreateRecordAsync(
                CreateRequest(
                    seed.Patient.Id,
                    seed.Product.Id,
                    seed.ScheduleItem.Id)));
    }

    [Test]
    public async Task GetRecordsAsync_ReturnsOnlyCurrentHospitalRecords()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        await AddVaccinationRecordAsync(dbContext, seed);

        var otherSeed = await SeedBaseDataAsync(
            dbContext,
            hospitalId: Guid.NewGuid(),
            patientNumber: "PT-OTHER",
            productCode: "PRIORIX-OTHER");
        await AddVaccinationRecordAsync(dbContext, otherSeed);

        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.GetRecordsAsync(new GetVaccinationRecordsRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items.Single().PatientId, Is.EqualTo(seed.Patient.Id));
        });
    }

    [Test]
    public async Task GetRecordsAsync_StatusFilter_ReturnsMatchingRecords()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var record = await AddVaccinationRecordAsync(dbContext, seed);
        record.Status = VaccinationRecordStatus.Cancelled;
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.GetRecordsAsync(
            new GetVaccinationRecordsRequest
            {
                Status = "Cancelled"
            });

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.Single().Status, Is.EqualTo("Cancelled"));
    }

    [Test]
    public async Task GetRecordAsync_WhenRecordExists_ReturnsRecord()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var record = await AddVaccinationRecordAsync(dbContext, seed);
        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.GetRecordAsync(record.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(record.Id));
            Assert.That(result.PatientNumber, Is.EqualTo(seed.Patient.PatientNumber));
            Assert.That(result.VaccineProductCode, Is.EqualTo(seed.Product.Code));
        });
    }

    [Test]
    public async Task GetMyRecordsAsync_ReturnsOnlyLinkedPatientRecords()
    {
        await using var dbContext = CreateDbContext();
        var patientUserId = Guid.NewGuid();

        var linkedSeed = await SeedBaseDataAsync(
            dbContext,
            patientNumber: "PT-LINKED",
            productCode: "LINKED-PRODUCT");
        await AddVaccinationRecordAsync(dbContext, linkedSeed);

        var otherSeed = await SeedBaseDataAsync(
            dbContext,
            patientNumber: "PT-OTHER",
            productCode: "OTHER-PRODUCT");
        await AddVaccinationRecordAsync(dbContext, otherSeed);

        dbContext.PatientPortalAccesses.Add(new PatientPortalAccess
        {
            UserId = patientUserId,
            PatientId = linkedSeed.Patient.Id
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            hospitalId: null,
            role: Role.Patient,
            userId: patientUserId);

        var result = await service.GetMyRecordsAsync(
            new GetVaccinationRecordsRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items.Single().PatientId, Is.EqualTo(linkedSeed.Patient.Id));
        });
    }

    [Test]
    public async Task GetMyRecordAsync_WhenRecordBelongsToUnlinkedPatient_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var patientUserId = Guid.NewGuid();

        var linkedSeed = await SeedBaseDataAsync(
            dbContext,
            patientNumber: "PT-LINKED",
            productCode: "LINKED-PRODUCT");
        var otherSeed = await SeedBaseDataAsync(
            dbContext,
            patientNumber: "PT-OTHER",
            productCode: "OTHER-PRODUCT");
        var otherRecord = await AddVaccinationRecordAsync(dbContext, otherSeed);

        dbContext.PatientPortalAccesses.Add(new PatientPortalAccess
        {
            UserId = patientUserId,
            PatientId = linkedSeed.Patient.Id
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            hospitalId: null,
            role: Role.Patient,
            userId: patientUserId);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.GetMyRecordAsync(otherRecord.Id));
    }

    [Test]
    public async Task GetMyRecordsAsync_WhenCurrentUserIsNotPatient_ThrowsForbiddenException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        await AddVaccinationRecordAsync(dbContext, seed);

        var service = CreateService(
            dbContext,
            seed.Hospital.Id,
            role: Role.Doctor);

        Assert.ThrowsAsync<ForbiddenException>(async () =>
            await service.GetMyRecordsAsync(
                new GetVaccinationRecordsRequest()));
    }

    [Test]
    public async Task UpdateRecordAsync_WithValidRequest_UpdatesRecord()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var record = await AddVaccinationRecordAsync(dbContext, seed, scheduleItemId: null);
        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.UpdateRecordAsync(
            record.Id,
            new UpdateVaccinationRecordRequest(
                seed.Product.Id,
                seed.ScheduleItem.Id,
                1,
                new DateOnly(2026, 1, 2),
                "BATCH-2",
                "Updated notes"));

        Assert.Multiple(() =>
        {
            Assert.That(result.BatchNumber, Is.EqualTo("BATCH-2"));
            Assert.That(result.Notes, Is.EqualTo("Updated notes"));
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });

        var updated = await dbContext.VaccinationRecords.SingleAsync(
            vaccinationRecord => vaccinationRecord.Id == record.Id);

        Assert.That(updated.UpdatedBy, Is.EqualTo(CurrentUserId.ToString()));
    }

    [Test]
    public async Task UpdateRecordAsync_WhenRecordCancelled_ThrowsBusinessRuleException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var record = await AddVaccinationRecordAsync(dbContext, seed);
        record.Status = VaccinationRecordStatus.Cancelled;
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, seed.Hospital.Id);

        Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.UpdateRecordAsync(
                record.Id,
                new UpdateVaccinationRecordRequest(
                    seed.Product.Id,
                    seed.ScheduleItem.Id,
                    1,
                    new DateOnly(2026, 1, 1),
                    null,
                    null)));
    }

    [Test]
    public async Task CancelRecordAsync_WithAdministeredRecord_CancelsRecord()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var record = await AddVaccinationRecordAsync(dbContext, seed);
        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.CancelRecordAsync(record.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Cancelled"));
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task MarkRecordEnteredInErrorAsync_WithAdministeredRecord_MarksRecord()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var record = await AddVaccinationRecordAsync(dbContext, seed);
        var service = CreateService(dbContext, seed.Hospital.Id);

        var result = await service.MarkRecordEnteredInErrorAsync(record.Id);

        Assert.That(result.Status, Is.EqualTo("EnteredInError"));
    }

    [Test]
    public async Task CancelRecordAsync_WhenAlreadyCancelled_ThrowsBusinessRuleException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedBaseDataAsync(dbContext);
        var record = await AddVaccinationRecordAsync(dbContext, seed);
        record.Status = VaccinationRecordStatus.Cancelled;
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, seed.Hospital.Id);

        Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.CancelRecordAsync(record.Id));
    }

    private static VaccinationRecordsService CreateService(
        VaccineTrackerDbContext dbContext,
        Guid? hospitalId,
        Role role = Role.Doctor,
        Guid? userId = null)
    {
        return new VaccinationRecordsService(
            dbContext,
            new TestCurrentUser(userId ?? CurrentUserId, hospitalId, role),
            NullLogger<VaccinationRecordsService>.Instance);
    }

    private static CreateVaccinationRecordRequest CreateRequest(
        Guid patientId,
        Guid productId,
        Guid? scheduleItemId,
        DateOnly? administeredDate = null)
    {
        return new CreateVaccinationRecordRequest(
            patientId,
            productId,
            scheduleItemId,
            1,
            administeredDate ?? new DateOnly(2026, 1, 1),
            "BATCH-1",
            "Initial dose");
    }

    private static async Task<RecordSeed> SeedBaseDataAsync(
        VaccineTrackerDbContext dbContext,
        Guid? hospitalId = null,
        string patientNumber = "PT-0001",
        string productCode = "PRIORIX")
    {
        var hospital = new Hospital
        {
            Id = hospitalId ?? Guid.NewGuid(),
            Name = $"Hospital {patientNumber}",
            IsActive = true
        };

        var patient = new Patient
        {
            HospitalId = hospital.Id,
            PatientNumber = patientNumber,
            FirstName = "Child",
            LastName = patientNumber,
            DateOfBirth = new DateOnly(2025, 1, 1),
            Gender = Gender.Female,
            Status = EntityStatus.Active
        };

        var vaccineType = new VaccineType
        {
            Name = $"MMR Vaccine {patientNumber}",
            Code = $"MMR-{patientNumber}",
            DiseaseTarget = "Measles Mumps Rubella",
            Status = EntityStatus.Active
        };

        var manufacturer = new VaccineManufacturer
        {
            Name = $"GSK {patientNumber}",
            Code = $"GSK-{patientNumber}",
            Status = EntityStatus.Active
        };

        dbContext.AddRange(hospital, patient, vaccineType, manufacturer);
        await dbContext.SaveChangesAsync();

        var product = new VaccineProduct
        {
            VaccineTypeId = vaccineType.Id,
            ManufacturerId = manufacturer.Id,
            Name = productCode,
            Code = productCode,
            Status = EntityStatus.Active
        };

        var scheduleItem = new VaccineScheduleItem
        {
            VaccineTypeId = vaccineType.Id,
            TargetGroup = VaccineTargetGroup.Child,
            DueAgeInDays = 270,
            DoseNumber = 1,
            Status = EntityStatus.Active
        };

        dbContext.AddRange(product, scheduleItem);
        await dbContext.SaveChangesAsync();

        return new RecordSeed(
            hospital,
            patient,
            vaccineType,
            manufacturer,
            product,
            scheduleItem);
    }

    private static async Task AddHospitalAsync(
        VaccineTrackerDbContext dbContext,
        Guid hospitalId)
    {
        dbContext.Hospitals.Add(new Hospital
        {
            Id = hospitalId,
            Name = "Other Hospital",
            IsActive = true
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<VaccinationRecord> AddVaccinationRecordAsync(
        VaccineTrackerDbContext dbContext,
        RecordSeed seed,
        Guid? scheduleItemId = default)
    {
        var record = new VaccinationRecord
        {
            PatientId = seed.Patient.Id,
            HospitalId = seed.Hospital.Id,
            VaccineProductId = seed.Product.Id,
            VaccineScheduleItemId = scheduleItemId == default ? seed.ScheduleItem.Id : scheduleItemId,
            DoseNumber = 1,
            AdministeredDate = new DateOnly(2026, 1, 1),
            AdministeredByUserId = CurrentUserId,
            BatchNumber = "BATCH-1",
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

    private sealed record RecordSeed(
        Hospital Hospital,
        Patient Patient,
        VaccineType VaccineType,
        VaccineManufacturer Manufacturer,
        VaccineProduct Product,
        VaccineScheduleItem ScheduleItem);

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(
            Guid userId,
            Guid? hospitalId,
            Role role)
        {
            UserId = userId;
            HospitalId = hospitalId;
            Role = role.ToString();
        }

        public Guid UserId { get; }

        public Guid? HospitalId { get; }

        public string Email => "doctor@test.com";

        public string Role { get; }
    }
}
