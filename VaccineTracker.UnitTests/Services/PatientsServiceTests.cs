using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Patients;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class PatientsServiceTests
{
    [Test]
    public async Task CreatePatientAsync_WithValidRequest_CreatesPatient()
    {
        await using var dbContext = CreateDbContext();
        var hospitalId = Guid.NewGuid();
        await AddHospitalAsync(dbContext, hospitalId);

        var service = CreateService(
            dbContext,
            hospitalId,
            Role.HospitalAdmin);

        var result = await service.CreatePatientAsync(
            CreateRequest(hospitalId: null));

        Assert.Multiple(() =>
        {
            Assert.That(result.HospitalId, Is.EqualTo(hospitalId));
            Assert.That(result.PatientNumber, Is.EqualTo("PAT-001"));
            Assert.That(result.Gender, Is.EqualTo("Female"));
            Assert.That(result.Status, Is.EqualTo("Active"));
        });

        var patient = await dbContext.Patients.SingleAsync();
        Assert.That(patient.CreatedBy, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CreatePatientAsync_WhenPatientNumberExists_ThrowsConflictException()
    {
        await using var dbContext = CreateDbContext();
        var hospitalId = Guid.NewGuid();
        await AddHospitalAsync(dbContext, hospitalId);

        dbContext.Patients.Add(new Patient
        {
            HospitalId = hospitalId,
            PatientNumber = "PAT-001",
            FirstName = "Existing",
            LastName = "Patient",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = Gender.Female
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            hospitalId,
            Role.HospitalAdmin);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await service.CreatePatientAsync(CreateRequest(hospitalId: null)));
    }

    [Test]
    public async Task GetPatientAsync_FromAnotherHospital_ThrowsForbiddenException()
    {
        await using var dbContext = CreateDbContext();
        var currentHospitalId = Guid.NewGuid();
        var patientHospitalId = Guid.NewGuid();
        await AddHospitalAsync(dbContext, currentHospitalId);
        await AddHospitalAsync(dbContext, patientHospitalId);

        var patient = new Patient
        {
            HospitalId = patientHospitalId,
            PatientNumber = "PAT-002",
            FirstName = "Other",
            LastName = "Patient",
            DateOfBirth = new DateOnly(1985, 5, 10),
            Gender = Gender.Male
        };
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            currentHospitalId,
            Role.Doctor);

        Assert.ThrowsAsync<ForbiddenException>(async () =>
            await service.GetPatientAsync(patient.Id));
    }

    [Test]
    public async Task GetPatientAsync_AsPlatformAdmin_ThrowsForbiddenException()
    {
        await using var dbContext = CreateDbContext();
        var hospitalId = Guid.NewGuid();
        await AddHospitalAsync(dbContext, hospitalId);

        var patient = new Patient
        {
            HospitalId = hospitalId,
            PatientNumber = "PAT-003",
            FirstName = "Private",
            LastName = "Patient",
            DateOfBirth = new DateOnly(1980, 4, 12),
            Gender = Gender.Female
        };
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            hospitalId,
            Role.PlatformAdmin);

        Assert.ThrowsAsync<ForbiddenException>(async () =>
            await service.GetPatientAsync(patient.Id));
    }

    private static PatientsService CreateService(
        VaccineTrackerDbContext dbContext,
        Guid? hospitalId,
        Role role)
    {
        return new PatientsService(
            dbContext,
            new TestCurrentUser(Guid.NewGuid(), hospitalId, role.ToString()),
            NullLogger<PatientsService>.Instance);
    }

    private static CreatePatientRequest CreateRequest(Guid? hospitalId)
    {
        return new CreatePatientRequest(
            hospitalId,
            "PAT-001",
            "Jane",
            "Doe",
            new DateOnly(1995, 6, 15),
            "Female",
            "NAT-001",
            "jane.doe@test.com",
            "0771234567",
            "1 Main Street",
            "Berlin",
            "10115",
            "Germany",
            "John Doe",
            "0777654321",
            false);
    }

    private static async Task AddHospitalAsync(
        VaccineTrackerDbContext dbContext,
        Guid hospitalId)
    {
        dbContext.Hospitals.Add(new Hospital
        {
            Id = hospitalId,
            Name = $"Hospital {hospitalId}",
            IsActive = true
        });

        await dbContext.SaveChangesAsync();
    }

    private static VaccineTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VaccineTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaccineTrackerDbContext(options);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(
            Guid userId,
            Guid? hospitalId,
            string role)
        {
            UserId = userId;
            HospitalId = hospitalId;
            Role = role;
        }

        public Guid UserId { get; }

        public Guid? HospitalId { get; }

        public string Email => "staff@test.com";

        public string Role { get; }
    }
}
