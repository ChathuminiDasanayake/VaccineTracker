using Microsoft.EntityFrameworkCore;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Contracts.Hospitals;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class HospitalsServiceTests
{
    [Test]
    public async Task GetHospitalAsync_WhenHospitalExists_ReturnsHospital()
    {
        await using var dbContext = CreateDbContext();

        var hospital = new Hospital
        {
            Name = "City Hospital",
            IsActive = true
        };
        dbContext.Hospitals.Add(hospital);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetHospitalAsync(hospital.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(hospital.Id));
            Assert.That(result.Name, Is.EqualTo("City Hospital"));
        });
    }

    [Test]
    public async Task GetHospitalAsync_WhenHospitalNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.GetHospitalAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task CreateHospitalAsync_WithValidRequest_CreatesActiveHospital()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.CreateHospitalAsync(
            new CreateHospitalRequest(
                " City Hospital ",
                " REG-001 ",
                " 123456789 ",
                " admin@hospital.test ",
                null,
                " 08:00-17:00 "));

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("City Hospital"));
            Assert.That(result.RegistrationNumber, Is.EqualTo("REG-001"));
            Assert.That(result.ContactPhone, Is.EqualTo("123456789"));
            Assert.That(result.ContactEmail, Is.EqualTo("admin@hospital.test"));
            Assert.That(result.OpeningHours, Is.EqualTo("08:00-17:00"));
            Assert.That(result.IsActive, Is.True);
        });
    }

    [Test]
    public async Task CreateHospitalAsync_WhenNameEmpty_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ValidationException>(async () =>
            await service.CreateHospitalAsync(
                new CreateHospitalRequest(
                    " ",
                    null,
                    null,
                    null,
                    null,
                    null)));
    }

    [Test]
    public async Task UpdateHospitalAsync_WithValidRequest_UpdatesHospital()
    {
        await using var dbContext = CreateDbContext();

        var hospital = new Hospital
        {
            Name = "City Hospital",
            IsActive = true
        };
        dbContext.Hospitals.Add(hospital);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.UpdateHospitalAsync(
            hospital.Id,
            new UpdateHospitalRequest(
                "Updated Hospital",
                "REG-002",
                null,
                null,
                null,
                null));

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Updated Hospital"));
            Assert.That(result.RegistrationNumber, Is.EqualTo("REG-002"));
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task UpdateHospitalAsync_WhenHospitalNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.UpdateHospitalAsync(
                Guid.NewGuid(),
                new UpdateHospitalRequest(
                    "City Hospital",
                    null,
                    null,
                    null,
                    null,
                    null)));
    }

    [Test]
    public async Task DeactivateHospitalAsync_WithActiveHospital_DeactivatesHospital()
    {
        await using var dbContext = CreateDbContext();

        var hospital = new Hospital
        {
            Name = "City Hospital",
            IsActive = true
        };
        dbContext.Hospitals.Add(hospital);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.DeactivateHospitalAsync(hospital.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsActive, Is.False);
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task ActivateHospitalAsync_WithInactiveHospital_ActivatesHospital()
    {
        await using var dbContext = CreateDbContext();

        var hospital = new Hospital
        {
            Name = "City Hospital",
            IsActive = false
        };
        dbContext.Hospitals.Add(hospital);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.ActivateHospitalAsync(hospital.Id);

        Assert.That(result.IsActive, Is.True);
    }

    [Test]
    public async Task DeactivateHospitalAsync_WhenAlreadyInactive_ThrowsBusinessRuleException()
    {
        await using var dbContext = CreateDbContext();

        var hospital = new Hospital
        {
            Name = "City Hospital",
            IsActive = false
        };
        dbContext.Hospitals.Add(hospital);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.DeactivateHospitalAsync(hospital.Id));
    }

    private static HospitalsService CreateService(VaccineTrackerDbContext dbContext)
    {
        return new HospitalsService(dbContext);
    }

    private static VaccineTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VaccineTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaccineTrackerDbContext(options);
    }
}
