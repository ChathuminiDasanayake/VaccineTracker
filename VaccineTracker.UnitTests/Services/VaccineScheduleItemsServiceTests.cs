using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.VaccineScheduleItems;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class VaccineScheduleItemsServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Test]
    public async Task CreateScheduleItemAsync_WithValidRequest_CreatesActiveScheduleItem()
    {
        await using var dbContext = CreateDbContext();
        var vaccineType = await SeedVaccineTypeAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateScheduleItemAsync(
            CreateRequest(vaccineType.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.VaccineTypeId, Is.EqualTo(vaccineType.Id));
            Assert.That(result.VaccineTypeCode, Is.EqualTo("MMR"));
            Assert.That(result.TargetGroup, Is.EqualTo("Child"));
            Assert.That(result.DueAgeInDays, Is.EqualTo(270));
            Assert.That(result.DoseNumber, Is.EqualTo(1));
            Assert.That(result.Status, Is.EqualTo("Active"));
        });

        var item = await dbContext.VaccineScheduleItems.SingleAsync();

        Assert.That(item.CreatedBy, Is.EqualTo(CurrentUserId.ToString()));
    }

    [Test]
    public async Task CreateScheduleItemAsync_WhenTargetGroupInvalid_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        var vaccineType = await SeedVaccineTypeAsync(dbContext);
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ValidationException>(async () =>
            await service.CreateScheduleItemAsync(
                CreateRequest(vaccineType.Id, targetGroup: "InvalidGroup")));
    }

    [Test]
    public async Task CreateScheduleItemAsync_WhenNoTimingProvided_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        var vaccineType = await SeedVaccineTypeAsync(dbContext);
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ValidationException>(async () =>
            await service.CreateScheduleItemAsync(
                new CreateVaccineScheduleItemRequest(
                    vaccineType.Id,
                    "Child",
                    null,
                    null,
                    null,
                    1,
                    null)));
    }

    [Test]
    public async Task CreateScheduleItemAsync_WhenVaccineTypeInactive_ThrowsBusinessRuleException()
    {
        await using var dbContext = CreateDbContext();
        var vaccineType = await SeedVaccineTypeAsync(dbContext, EntityStatus.Inactive);
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.CreateScheduleItemAsync(
                CreateRequest(vaccineType.Id)));
    }

    [Test]
    public async Task CreateScheduleItemAsync_WhenDoseAlreadyExists_ThrowsConflictException()
    {
        await using var dbContext = CreateDbContext();
        var vaccineType = await SeedVaccineTypeAsync(dbContext);

        dbContext.VaccineScheduleItems.Add(new VaccineScheduleItem
        {
            VaccineTypeId = vaccineType.Id,
            TargetGroup = VaccineTargetGroup.Child,
            DueAgeInDays = 270,
            DoseNumber = 1,
            Status = EntityStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await service.CreateScheduleItemAsync(
                CreateRequest(vaccineType.Id)));
    }

    [Test]
    public async Task GetScheduleItemsAsync_ReturnsOnlyActiveItemsByDefault()
    {
        await using var dbContext = CreateDbContext();
        await SeedScheduleItemsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetScheduleItemsAsync(
            new GetVaccineScheduleItemsRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(2));
            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.Items.All(item => item.Status == "Active"), Is.True);
        });
    }

    [Test]
    public async Task GetScheduleItemsAsync_TargetGroupFilter_ReturnsMatchingItems()
    {
        await using var dbContext = CreateDbContext();
        await SeedScheduleItemsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetScheduleItemsAsync(
            new GetVaccineScheduleItemsRequest
            {
                TargetGroup = "Adult"
            });

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.Single().TargetGroup, Is.EqualTo("Adult"));
    }

    [Test]
    public async Task GetScheduleItemsAsync_StatusFilter_ReturnsInactiveItems()
    {
        await using var dbContext = CreateDbContext();
        await SeedScheduleItemsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetScheduleItemsAsync(
            new GetVaccineScheduleItemsRequest
            {
                Status = "Inactive"
            });

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.Single().Status, Is.EqualTo("Inactive"));
    }

    [Test]
    public async Task GetScheduleItemAsync_WhenItemExists_ReturnsItem()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScheduleItemsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetScheduleItemAsync(seed.ChildDose1.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(seed.ChildDose1.Id));
            Assert.That(result.VaccineTypeCode, Is.EqualTo("MMR"));
            Assert.That(result.TargetGroup, Is.EqualTo("Child"));
        });
    }

    [Test]
    public async Task UpdateScheduleItemAsync_WithValidRequest_UpdatesItem()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScheduleItemsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.UpdateScheduleItemAsync(
            seed.ChildDose1.Id,
            new UpdateVaccineScheduleItemRequest(
                seed.Mmr.Id,
                "Child",
                300,
                null,
                null,
                1,
                "Updated schedule"));

        Assert.Multiple(() =>
        {
            Assert.That(result.DueAgeInDays, Is.EqualTo(300));
            Assert.That(result.Description, Is.EqualTo("Updated schedule"));
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });

        var item = await dbContext.VaccineScheduleItems.SingleAsync(
            item => item.Id == seed.ChildDose1.Id);

        Assert.That(item.UpdatedBy, Is.EqualTo(CurrentUserId.ToString()));
    }

    [Test]
    public async Task UpdateScheduleItemAsync_WhenItemNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var vaccineType = await SeedVaccineTypeAsync(dbContext);
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.UpdateScheduleItemAsync(
                Guid.NewGuid(),
                new UpdateVaccineScheduleItemRequest(
                    vaccineType.Id,
                    "Child",
                    270,
                    null,
                    null,
                    1,
                    null)));
    }

    [Test]
    public async Task DeactivateScheduleItemAsync_WithActiveItem_DeactivatesItem()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScheduleItemsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.DeactivateScheduleItemAsync(seed.ChildDose1.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Inactive"));
            Assert.That(result.UpdatedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task ActivateScheduleItemAsync_WithInactiveItem_ActivatesItem()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScheduleItemsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.ActivateScheduleItemAsync(seed.InactiveDose.Id);

        Assert.That(result.Status, Is.EqualTo("Active"));
    }

    [Test]
    public async Task DeactivateScheduleItemAsync_WhenAlreadyInactive_ThrowsBusinessRuleException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScheduleItemsAsync(dbContext);
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.DeactivateScheduleItemAsync(seed.InactiveDose.Id));
    }

    private static VaccineScheduleItemsService CreateService(
        VaccineTrackerDbContext dbContext)
    {
        return new VaccineScheduleItemsService(
            dbContext,
            new TestCurrentUser(CurrentUserId),
            NullLogger<VaccineScheduleItemsService>.Instance);
    }

    private static CreateVaccineScheduleItemRequest CreateRequest(
        Guid vaccineTypeId,
        string targetGroup = "Child",
        int dueAgeInDays = 270,
        int doseNumber = 1)
    {
        return new CreateVaccineScheduleItemRequest(
            vaccineTypeId,
            targetGroup,
            dueAgeInDays,
            null,
            null,
            doseNumber,
            "Dose schedule");
    }

    private static async Task<VaccineType> SeedVaccineTypeAsync(
        VaccineTrackerDbContext dbContext,
        EntityStatus status = EntityStatus.Active)
    {
        var vaccineType = new VaccineType
        {
            Name = "MMR Vaccine",
            Code = "MMR",
            DiseaseTarget = "Measles Mumps Rubella",
            Status = status
        };

        dbContext.VaccineTypes.Add(vaccineType);
        await dbContext.SaveChangesAsync();

        return vaccineType;
    }

    private static async Task<ScheduleSeed> SeedScheduleItemsAsync(
        VaccineTrackerDbContext dbContext)
    {
        var mmr = await SeedVaccineTypeAsync(dbContext);

        var childDose1 = new VaccineScheduleItem
        {
            VaccineTypeId = mmr.Id,
            TargetGroup = VaccineTargetGroup.Child,
            DueAgeInDays = 270,
            DoseNumber = 1,
            Description = "MMR child dose 1",
            Status = EntityStatus.Active
        };

        var adultDose1 = new VaccineScheduleItem
        {
            VaccineTypeId = mmr.Id,
            TargetGroup = VaccineTargetGroup.Adult,
            MinimumAgeInYears = 18,
            DoseNumber = 1,
            Description = "MMR adult dose 1",
            Status = EntityStatus.Active
        };

        var inactiveDose = new VaccineScheduleItem
        {
            VaccineTypeId = mmr.Id,
            TargetGroup = VaccineTargetGroup.Child,
            DueAgeInDays = 450,
            DoseNumber = 2,
            Description = "Old dose",
            Status = EntityStatus.Inactive
        };

        dbContext.VaccineScheduleItems.AddRange(
            childDose1,
            adultDose1,
            inactiveDose);
        await dbContext.SaveChangesAsync();

        return new ScheduleSeed(mmr, childDose1, adultDose1, inactiveDose);
    }

    private static VaccineTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VaccineTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaccineTrackerDbContext(options);
    }

    private sealed record ScheduleSeed(
        VaccineType Mmr,
        VaccineScheduleItem ChildDose1,
        VaccineScheduleItem AdultDose1,
        VaccineScheduleItem InactiveDose);

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
