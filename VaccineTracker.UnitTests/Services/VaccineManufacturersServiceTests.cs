using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Contracts.VaccineManufacturers;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class VaccineManufacturersServiceTests
{
    [Test]
    public async Task CreateManufacturerAsync_WithValidRequest_CreatesActiveManufacturer()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.CreateManufacturerAsync(
            CreateRequest(
                name: "Serum Institute",
                code: " sii "));

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Serum Institute"));
            Assert.That(result.Code, Is.EqualTo("SII"));
            Assert.That(result.Status, Is.EqualTo("Active"));
        });

        var manufacturer = await dbContext.VaccineManufacturers.SingleAsync();

        Assert.That(manufacturer.Code, Is.EqualTo("SII"));
    }

    [Test]
    public async Task CreateManufacturerAsync_WhenCodeAlreadyExists_ThrowsConflictException()
    {
        await using var dbContext = CreateDbContext();

        dbContext.VaccineManufacturers.Add(new VaccineManufacturer
        {
            Name = "Serum Institute",
            Code = "SII",
            Status = EntityStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await service.CreateManufacturerAsync(
                CreateRequest(
                    name: "Duplicate Serum Institute",
                    code: "sii")));
    }

    [Test]
    public async Task GetManufacturersAsync_ReturnsOnlyActiveManufacturersByDefault()
    {
        await using var dbContext = CreateDbContext();

        AddManufacturers(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetManufacturersAsync(
            new GetVaccineManufacturersRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(2));
            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.TotalPages, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetManufacturersAsync_Search_ReturnsMatchingManufacturer()
    {
        await using var dbContext = CreateDbContext();

        AddManufacturers(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetManufacturersAsync(
            new GetVaccineManufacturersRequest
            {
                Search = "Serum"
            });

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.Single().Code, Is.EqualTo("SII"));
    }

    [Test]
    public async Task GetManufacturersAsync_StatusFilter_ReturnsMatchingManufacturers()
    {
        await using var dbContext = CreateDbContext();

        AddManufacturers(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetManufacturersAsync(
            new GetVaccineManufacturersRequest
            {
                Status = "Inactive"
            });

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.Single().Status, Is.EqualTo("Inactive"));
    }

    [Test]
    public async Task GetManufacturersAsync_WhenStatusInvalid_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ValidationException>(async () =>
            await service.GetManufacturersAsync(
                new GetVaccineManufacturersRequest
                {
                    Status = "InvalidStatus"
                }));
    }

    [Test]
    public async Task GetManufacturerAsync_WhenManufacturerExists_ReturnsManufacturer()
    {
        await using var dbContext = CreateDbContext();

        var manufacturer = new VaccineManufacturer
        {
            Name = "Serum Institute",
            Code = "SII",
            Status = EntityStatus.Active
        };
        dbContext.VaccineManufacturers.Add(manufacturer);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetManufacturerAsync(manufacturer.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(manufacturer.Id));
            Assert.That(result.Code, Is.EqualTo("SII"));
        });
    }

    [Test]
    public async Task GetManufacturerAsync_WhenManufacturerNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.GetManufacturerAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task UpdateManufacturerAsync_WithValidRequest_UpdatesManufacturer()
    {
        await using var dbContext = CreateDbContext();

        var manufacturer = new VaccineManufacturer
        {
            Name = "Serum Institute",
            Code = "SII",
            Description = "Old description",
            Status = EntityStatus.Active
        };
        dbContext.VaccineManufacturers.Add(manufacturer);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.UpdateManufacturerAsync(
            manufacturer.Id,
            new UpdateVaccineManufacturerRequest(
                "Updated Serum Institute",
                "sii-updated",
                "Updated description"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Updated Serum Institute"));
            Assert.That(result.Code, Is.EqualTo("SII-UPDATED"));
            Assert.That(result.Description, Is.EqualTo("Updated description"));
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task UpdateManufacturerAsync_WhenManufacturerNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.UpdateManufacturerAsync(
                Guid.NewGuid(),
                new UpdateVaccineManufacturerRequest(
                    "Serum Institute",
                    "SII",
                    null)));
    }

    [Test]
    public async Task UpdateManufacturerAsync_WhenCodeAlreadyExists_ThrowsConflictException()
    {
        await using var dbContext = CreateDbContext();

        var existing = new VaccineManufacturer
        {
            Name = "Serum Institute",
            Code = "SII",
            Status = EntityStatus.Active
        };

        var target = new VaccineManufacturer
        {
            Name = "GSK",
            Code = "GSK",
            Status = EntityStatus.Active
        };

        dbContext.VaccineManufacturers.AddRange(existing, target);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await service.UpdateManufacturerAsync(
                target.Id,
                new UpdateVaccineManufacturerRequest(
                    "Updated GSK",
                    "sii",
                    null)));
    }

    [Test]
    public async Task DeactivateManufacturerAsync_WithActiveManufacturer_DeactivatesManufacturer()
    {
        await using var dbContext = CreateDbContext();

        var manufacturer = new VaccineManufacturer
        {
            Name = "Serum Institute",
            Code = "SII",
            Status = EntityStatus.Active
        };
        dbContext.VaccineManufacturers.Add(manufacturer);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.DeactivateManufacturerAsync(manufacturer.Id);

        Assert.That(result.Status, Is.EqualTo("Inactive"));
    }

    [Test]
    public async Task ActivateManufacturerAsync_WithInactiveManufacturer_ActivatesManufacturer()
    {
        await using var dbContext = CreateDbContext();

        var manufacturer = new VaccineManufacturer
        {
            Name = "Serum Institute",
            Code = "SII",
            Status = EntityStatus.Inactive
        };
        dbContext.VaccineManufacturers.Add(manufacturer);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.ActivateManufacturerAsync(manufacturer.Id);

        Assert.That(result.Status, Is.EqualTo("Active"));
    }

    [Test]
    public async Task DeactivateManufacturerAsync_WhenAlreadyInactive_ThrowsBusinessRuleException()
    {
        await using var dbContext = CreateDbContext();

        var manufacturer = new VaccineManufacturer
        {
            Name = "Serum Institute",
            Code = "SII",
            Status = EntityStatus.Inactive
        };
        dbContext.VaccineManufacturers.Add(manufacturer);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.DeactivateManufacturerAsync(manufacturer.Id));
    }

    private static VaccineManufacturersService CreateService(
        VaccineTrackerDbContext dbContext)
    {
        return new VaccineManufacturersService(
            dbContext,
            NullLogger<VaccineManufacturersService>.Instance);
    }

    private static CreateVaccineManufacturerRequest CreateRequest(
        string name = "Serum Institute",
        string code = "SII",
        string? description = null)
    {
        return new CreateVaccineManufacturerRequest(
            name,
            code,
            description);
    }

    private static void AddManufacturers(VaccineTrackerDbContext dbContext)
    {
        dbContext.VaccineManufacturers.AddRange(
            new VaccineManufacturer
            {
                Name = "Serum Institute",
                Code = "SII",
                Status = EntityStatus.Active
            },
            new VaccineManufacturer
            {
                Name = "GlaxoSmithKline",
                Code = "GSK",
                Status = EntityStatus.Active
            },
            new VaccineManufacturer
            {
                Name = "Inactive Manufacturer",
                Code = "OLD",
                Status = EntityStatus.Inactive
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
