using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Contracts.Vaccines;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class VaccinesServiceTests
{
    [Test]
    public async Task CreateVaccineAsync_WithValidRequest_CreatesActiveVaccineType()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.CreateVaccineAsync(
            CreateRequest(
                name: "BCG Vaccine",
                code: " bcg ",
                diseaseTarget: "Tuberculosis"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("BCG Vaccine"));
            Assert.That(result.Code, Is.EqualTo("BCG"));
            Assert.That(result.DiseaseTarget, Is.EqualTo("Tuberculosis"));
            Assert.That(result.Status, Is.EqualTo("Active"));
        });

        var vaccineType = await dbContext.VaccineTypes.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(vaccineType.Code, Is.EqualTo("BCG"));
            Assert.That(vaccineType.Status, Is.EqualTo(EntityStatus.Active));
        });
    }

    [Test]
    public async Task CreateVaccineAsync_WhenCodeAlreadyExists_ThrowsConflictException()
    {
        await using var dbContext = CreateDbContext();

        dbContext.VaccineTypes.Add(new VaccineType
        {
            Name = "BCG Vaccine",
            Code = "BCG",
            DiseaseTarget = "Tuberculosis",
            Status = EntityStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await service.CreateVaccineAsync(
                CreateRequest(
                    name: "Another BCG",
                    code: "bcg",
                    diseaseTarget: "Tuberculosis")));
    }

    [Test]
    public async Task GetVaccinesAsync_ReturnsOnlyActiveVaccineTypesByDefault()
    {
        await using var dbContext = CreateDbContext();

        AddVaccineTypes(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetVaccinesAsync(
            new GetVaccinesRequest
            {
                PageNumber = 1,
                PageSize = 2
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(2));
            Assert.That(result.PageNumber, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(2));
            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.TotalPages, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetVaccinesAsync_Search_ReturnsMatchingVaccineType()
    {
        await using var dbContext = CreateDbContext();

        AddVaccineTypes(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetVaccinesAsync(
            new GetVaccinesRequest
            {
                Search = "Measles"
            });

        Assert.That(result.Items, Has.Count.EqualTo(1));

        var vaccine = result.Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(vaccine.Code, Is.EqualTo("MMR"));
            Assert.That(result.TotalCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetVaccinesAsync_StatusFilter_ReturnsMatchingVaccineTypes()
    {
        await using var dbContext = CreateDbContext();

        AddVaccineTypes(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetVaccinesAsync(
            new GetVaccinesRequest
            {
                Status = "Inactive"
            });

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.Single().Status, Is.EqualTo("Inactive"));
    }

    [Test]
    public async Task GetVaccinesAsync_WhenStatusInvalid_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ValidationException>(async () =>
            await service.GetVaccinesAsync(
                new GetVaccinesRequest
                {
                    Status = "InvalidStatus"
                }));
    }

    [Test]
    public async Task GetVaccineAsync_WhenVaccineTypeExists_ReturnsVaccineType()
    {
        await using var dbContext = CreateDbContext();

        var vaccineType = new VaccineType
        {
            Name = "BCG Vaccine",
            Code = "BCG",
            DiseaseTarget = "Tuberculosis",
            Status = EntityStatus.Active
        };
        dbContext.VaccineTypes.Add(vaccineType);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetVaccineAsync(vaccineType.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(vaccineType.Id));
            Assert.That(result.Code, Is.EqualTo("BCG"));
            Assert.That(result.DiseaseTarget, Is.EqualTo("Tuberculosis"));
        });
    }

    [Test]
    public async Task GetVaccineAsync_WhenVaccineTypeNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.GetVaccineAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task UpdateVaccineAsync_WithValidRequest_UpdatesVaccineType()
    {
        await using var dbContext = CreateDbContext();

        var vaccineType = new VaccineType
        {
            Name = "BCG Vaccine",
            Code = "BCG",
            DiseaseTarget = "Tuberculosis",
            Description = "Old description",
            Status = EntityStatus.Active
        };
        dbContext.VaccineTypes.Add(vaccineType);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.UpdateVaccineAsync(
            vaccineType.Id,
            new UpdateVaccineRequest(
                "Updated BCG Vaccine",
                "bcg-updated",
                "Tuberculosis",
                "Updated description"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Updated BCG Vaccine"));
            Assert.That(result.Code, Is.EqualTo("BCG-UPDATED"));
            Assert.That(result.Description, Is.EqualTo("Updated description"));
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });

        var updated = await dbContext.VaccineTypes.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(updated.Code, Is.EqualTo("BCG-UPDATED"));
            Assert.That(updated.UpdatedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task UpdateVaccineAsync_WhenVaccineTypeNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.UpdateVaccineAsync(
                Guid.NewGuid(),
                new UpdateVaccineRequest(
                    "BCG Vaccine",
                    "BCG",
                    "Tuberculosis",
                    null)));
    }

    [Test]
    public async Task UpdateVaccineAsync_WhenCodeAlreadyExists_ThrowsConflictException()
    {
        await using var dbContext = CreateDbContext();

        var existing = new VaccineType
        {
            Name = "BCG Vaccine",
            Code = "BCG",
            DiseaseTarget = "Tuberculosis",
            Status = EntityStatus.Active
        };

        var target = new VaccineType
        {
            Name = "MMR Vaccine",
            Code = "MMR",
            DiseaseTarget = "Measles",
            Status = EntityStatus.Active
        };

        dbContext.VaccineTypes.AddRange(existing, target);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await service.UpdateVaccineAsync(
                target.Id,
                new UpdateVaccineRequest(
                    "Updated MMR",
                    "bcg",
                    "Measles",
                    null)));
    }

    [Test]
    public async Task DeactivateVaccineAsync_WithActiveVaccineType_DeactivatesVaccineType()
    {
        await using var dbContext = CreateDbContext();

        var vaccineType = new VaccineType
        {
            Name = "BCG Vaccine",
            Code = "BCG",
            DiseaseTarget = "Tuberculosis",
            Status = EntityStatus.Active
        };
        dbContext.VaccineTypes.Add(vaccineType);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.DeactivateVaccineAsync(vaccineType.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Inactive"));
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });

        var updated = await dbContext.VaccineTypes.SingleAsync();

        Assert.That(updated.Status, Is.EqualTo(EntityStatus.Inactive));
    }

    [Test]
    public async Task ActivateVaccineAsync_WithInactiveVaccineType_ActivatesVaccineType()
    {
        await using var dbContext = CreateDbContext();

        var vaccineType = new VaccineType
        {
            Name = "BCG Vaccine",
            Code = "BCG",
            DiseaseTarget = "Tuberculosis",
            Status = EntityStatus.Inactive
        };
        dbContext.VaccineTypes.Add(vaccineType);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.ActivateVaccineAsync(vaccineType.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Active"));
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });

        var updated = await dbContext.VaccineTypes.SingleAsync();

        Assert.That(updated.Status, Is.EqualTo(EntityStatus.Active));
    }

    [Test]
    public async Task DeactivateVaccineAsync_WhenVaccineTypeAlreadyInactive_ThrowsBusinessRuleException()
    {
        await using var dbContext = CreateDbContext();

        var vaccineType = new VaccineType
        {
            Name = "BCG Vaccine",
            Code = "BCG",
            DiseaseTarget = "Tuberculosis",
            Status = EntityStatus.Inactive
        };
        dbContext.VaccineTypes.Add(vaccineType);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.DeactivateVaccineAsync(vaccineType.Id));
    }

    [Test]
    public async Task ActivateVaccineAsync_WhenVaccineTypeNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.ActivateVaccineAsync(Guid.NewGuid()));
    }

    private static VaccinesService CreateService(VaccineTrackerDbContext dbContext)
    {
        return new VaccinesService(
            dbContext,
            NullLogger<VaccinesService>.Instance);
    }

    private static CreateVaccineRequest CreateRequest(
        string name = "BCG Vaccine",
        string code = "BCG",
        string diseaseTarget = "Tuberculosis",
        string? description = null)
    {
        return new CreateVaccineRequest(
            name,
            code,
            diseaseTarget,
            description);
    }

    private static void AddVaccineTypes(VaccineTrackerDbContext dbContext)
    {
        dbContext.VaccineTypes.AddRange(
            new VaccineType
            {
                Name = "BCG Vaccine",
                Code = "BCG",
                DiseaseTarget = "Tuberculosis",
                Status = EntityStatus.Active
            },
            new VaccineType
            {
                Name = "Hepatitis B Vaccine",
                Code = "HEPB",
                DiseaseTarget = "Hepatitis B",
                Status = EntityStatus.Inactive
            },
            new VaccineType
            {
                Name = "MMR Vaccine",
                Code = "MMR",
                DiseaseTarget = "Measles Mumps Rubella",
                Status = EntityStatus.Active
            });
    }

    private static VaccineTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VaccineTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaccineTrackerDbContext(options);
    }
}
