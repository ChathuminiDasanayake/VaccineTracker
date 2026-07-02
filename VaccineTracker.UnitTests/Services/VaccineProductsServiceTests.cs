using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.VaccineProducts;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class VaccineProductsServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Test]
    public async Task CreateProductAsync_WithValidRequest_CreatesActiveProduct()
    {
        await using var dbContext = CreateDbContext();
        var (vaccineType, manufacturer) = await SeedActiveTypeAndManufacturerAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateProductAsync(
            CreateRequest(vaccineType.Id, manufacturer.Id, code: " priorix "));

        Assert.Multiple(() =>
        {
            Assert.That(result.VaccineTypeId, Is.EqualTo(vaccineType.Id));
            Assert.That(result.VaccineTypeCode, Is.EqualTo("MMR"));
            Assert.That(result.ManufacturerId, Is.EqualTo(manufacturer.Id));
            Assert.That(result.ManufacturerCode, Is.EqualTo("GSK"));
            Assert.That(result.Code, Is.EqualTo("PRIORIX"));
            Assert.That(result.Status, Is.EqualTo("Active"));
        });

        var product = await dbContext.VaccineProducts.SingleAsync();

        Assert.That(product.CreatedBy, Is.EqualTo(CurrentUserId.ToString()));
    }

    [Test]
    public async Task CreateProductAsync_WhenCodeAlreadyExists_ThrowsConflictException()
    {
        await using var dbContext = CreateDbContext();
        var (vaccineType, manufacturer) = await SeedActiveTypeAndManufacturerAsync(dbContext);

        dbContext.VaccineProducts.Add(new VaccineProduct
        {
            VaccineTypeId = vaccineType.Id,
            ManufacturerId = manufacturer.Id,
            Name = "Priorix",
            Code = "PRIORIX",
            Status = EntityStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await service.CreateProductAsync(
                CreateRequest(vaccineType.Id, manufacturer.Id, code: "priorix")));
    }

    [Test]
    public async Task CreateProductAsync_WhenVaccineTypeInactive_ThrowsBusinessRuleException()
    {
        await using var dbContext = CreateDbContext();
        var manufacturer = new VaccineManufacturer
        {
            Name = "GSK",
            Code = "GSK",
            Status = EntityStatus.Active
        };
        var vaccineType = new VaccineType
        {
            Name = "MMR Vaccine",
            Code = "MMR",
            DiseaseTarget = "Measles Mumps Rubella",
            Status = EntityStatus.Inactive
        };
        dbContext.AddRange(vaccineType, manufacturer);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.CreateProductAsync(
                CreateRequest(vaccineType.Id, manufacturer.Id)));
    }

    [Test]
    public async Task CreateProductAsync_WhenManufacturerNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var (vaccineType, _) = await SeedActiveTypeAndManufacturerAsync(dbContext);
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.CreateProductAsync(
                CreateRequest(vaccineType.Id, Guid.NewGuid())));
    }

    [Test]
    public async Task GetProductsAsync_ReturnsOnlyActiveProductsByDefault()
    {
        await using var dbContext = CreateDbContext();
        await SeedProductsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetProductsAsync(new GetVaccineProductsRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(2));
            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.Items.All(product => product.Status == "Active"), Is.True);
        });
    }

    [Test]
    public async Task GetProductsAsync_Search_ReturnsMatchingProduct()
    {
        await using var dbContext = CreateDbContext();
        await SeedProductsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetProductsAsync(
            new GetVaccineProductsRequest
            {
                Search = "Priorix"
            });

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.Single().Code, Is.EqualTo("PRIORIX"));
    }

    [Test]
    public async Task GetProductsAsync_ManufacturerFilter_ReturnsMatchingProducts()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProductsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetProductsAsync(
            new GetVaccineProductsRequest
            {
                ManufacturerId = seed.Gsk.Id
            });

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.Single().ManufacturerCode, Is.EqualTo("GSK"));
    }

    [Test]
    public async Task GetProductsAsync_StatusFilter_ReturnsInactiveProducts()
    {
        await using var dbContext = CreateDbContext();
        await SeedProductsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetProductsAsync(
            new GetVaccineProductsRequest
            {
                Status = "Inactive"
            });

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.Single().Status, Is.EqualTo("Inactive"));
    }

    [Test]
    public async Task GetProductsAsync_WhenStatusInvalid_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ValidationException>(async () =>
            await service.GetProductsAsync(
                new GetVaccineProductsRequest
                {
                    Status = "Invalid"
                }));
    }

    [Test]
    public async Task GetProductAsync_WhenProductExists_ReturnsProduct()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProductsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetProductAsync(seed.Priorix.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(seed.Priorix.Id));
            Assert.That(result.Code, Is.EqualTo("PRIORIX"));
            Assert.That(result.VaccineTypeCode, Is.EqualTo("MMR"));
            Assert.That(result.ManufacturerCode, Is.EqualTo("GSK"));
        });
    }

    [Test]
    public async Task UpdateProductAsync_WithValidRequest_UpdatesProduct()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProductsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.UpdateProductAsync(
            seed.Priorix.Id,
            new UpdateVaccineProductRequest(
                seed.Mmr.Id,
                seed.Gsk.Id,
                "Updated Priorix",
                "priorix-updated",
                "Updated description"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Updated Priorix"));
            Assert.That(result.Code, Is.EqualTo("PRIORIX-UPDATED"));
            Assert.That(result.Description, Is.EqualTo("Updated description"));
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });

        var product = await dbContext.VaccineProducts.SingleAsync(
            product => product.Id == seed.Priorix.Id);

        Assert.That(product.UpdatedBy, Is.EqualTo(CurrentUserId.ToString()));
    }

    [Test]
    public async Task UpdateProductAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var (vaccineType, manufacturer) = await SeedActiveTypeAndManufacturerAsync(dbContext);
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.UpdateProductAsync(
                Guid.NewGuid(),
                new UpdateVaccineProductRequest(
                    vaccineType.Id,
                    manufacturer.Id,
                    "Priorix",
                    "PRIORIX",
                    null)));
    }

    [Test]
    public async Task DeactivateProductAsync_WithActiveProduct_DeactivatesProduct()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProductsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.DeactivateProductAsync(seed.Priorix.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Inactive"));
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });

        var product = await dbContext.VaccineProducts.SingleAsync(
            product => product.Id == seed.Priorix.Id);

        Assert.That(product.UpdatedBy, Is.EqualTo(CurrentUserId.ToString()));
    }

    [Test]
    public async Task ActivateProductAsync_WithInactiveProduct_ActivatesProduct()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProductsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.ActivateProductAsync(seed.OldProduct.Id);

        Assert.That(result.Status, Is.EqualTo("Active"));
    }

    [Test]
    public async Task DeactivateProductAsync_WhenAlreadyInactive_ThrowsBusinessRuleException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProductsAsync(dbContext);
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.DeactivateProductAsync(seed.OldProduct.Id));
    }

    private static VaccineProductsService CreateService(
        VaccineTrackerDbContext dbContext)
    {
        return new VaccineProductsService(
            dbContext,
            new TestCurrentUser(CurrentUserId),
            NullLogger<VaccineProductsService>.Instance);
    }

    private static CreateVaccineProductRequest CreateRequest(
        Guid vaccineTypeId,
        Guid manufacturerId,
        string name = "Priorix",
        string code = "PRIORIX",
        string? description = null)
    {
        return new CreateVaccineProductRequest(
            vaccineTypeId,
            manufacturerId,
            name,
            code,
            description);
    }

    private static async Task<(VaccineType VaccineType, VaccineManufacturer Manufacturer)>
        SeedActiveTypeAndManufacturerAsync(VaccineTrackerDbContext dbContext)
    {
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

        dbContext.AddRange(vaccineType, manufacturer);
        await dbContext.SaveChangesAsync();

        return (vaccineType, manufacturer);
    }

    private static async Task<ProductSeed> SeedProductsAsync(
        VaccineTrackerDbContext dbContext)
    {
        var mmr = new VaccineType
        {
            Name = "MMR Vaccine",
            Code = "MMR",
            DiseaseTarget = "Measles Mumps Rubella",
            Status = EntityStatus.Active
        };

        var bcg = new VaccineType
        {
            Name = "BCG Vaccine",
            Code = "BCG",
            DiseaseTarget = "Tuberculosis",
            Status = EntityStatus.Active
        };

        var gsk = new VaccineManufacturer
        {
            Name = "GSK",
            Code = "GSK",
            Status = EntityStatus.Active
        };

        var serum = new VaccineManufacturer
        {
            Name = "Serum Institute",
            Code = "SII",
            Status = EntityStatus.Active
        };

        dbContext.AddRange(mmr, bcg, gsk, serum);
        await dbContext.SaveChangesAsync();

        var priorix = new VaccineProduct
        {
            VaccineTypeId = mmr.Id,
            ManufacturerId = gsk.Id,
            Name = "Priorix",
            Code = "PRIORIX",
            Status = EntityStatus.Active
        };

        var bcgProduct = new VaccineProduct
        {
            VaccineTypeId = bcg.Id,
            ManufacturerId = serum.Id,
            Name = "BCG Serum",
            Code = "BCG-SII",
            Status = EntityStatus.Active
        };

        var oldProduct = new VaccineProduct
        {
            VaccineTypeId = mmr.Id,
            ManufacturerId = serum.Id,
            Name = "Old MMR",
            Code = "OLD-MMR",
            Status = EntityStatus.Inactive
        };

        dbContext.AddRange(priorix, bcgProduct, oldProduct);
        await dbContext.SaveChangesAsync();

        return new ProductSeed(
            mmr,
            bcg,
            gsk,
            serum,
            priorix,
            bcgProduct,
            oldProduct);
    }

    private static VaccineTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VaccineTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaccineTrackerDbContext(options);
    }

    private sealed record ProductSeed(
        VaccineType Mmr,
        VaccineType Bcg,
        VaccineManufacturer Gsk,
        VaccineManufacturer Serum,
        VaccineProduct Priorix,
        VaccineProduct BcgProduct,
        VaccineProduct OldProduct);

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; }

        public Guid? HospitalId => null;

        public string Email => "platform.admin@test.com";

        public string Role => Domain.Enums.Role.PlatformAdmin.ToString();
    }
}
