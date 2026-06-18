using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Users;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Authentication;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class UsersServiceTests
{
    [Test]
    public async Task CreateHospitalUserAsync_WithValidRequest_CreatesActiveHospitalUser()
    {
        await using var dbContext = CreateDbContext();
        var hospitalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        dbContext.Hospitals.Add(new Hospital
        {
            Id = hospitalId,
            Name = "Central Hospital",
            IsActive = true
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, currentUserId, hospitalId, Role.HospitalAdmin);

        var result = await service.CreateHospitalUserAsync(new CreateHospitalUserRequest(
            "doctor.one",
            "doctor.one@test.com",
            "Password@123",
            "Doctor",
            "One",
            null,
            "Doctor",
            "Male",
            "0771234567",
            "EMP-001"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Username, Is.EqualTo("doctor.one"));
            Assert.That(result.Email, Is.EqualTo("doctor.one@test.com"));
            Assert.That(result.HospitalId, Is.EqualTo(hospitalId));
            Assert.That(result.Roles, Is.EquivalentTo(new[] { "Doctor" }));
            Assert.That(result.Status, Is.EqualTo("Active"));
        });

        var user = await dbContext.Users.SingleAsync(user => user.Email == "doctor.one@test.com");
        Assert.Multiple(() =>
        {
            Assert.That(user.NormalizedUsername, Is.EqualTo("DOCTOR.ONE"));
            Assert.That(user.NormalizedEmail, Is.EqualTo("DOCTOR.ONE@TEST.COM"));
            Assert.That(user.PasswordHash, Does.StartWith("pbkdf2-sha256."));
            Assert.That(user.CreatedBy, Is.EqualTo(currentUserId.ToString()));
        });
    }

    [Test]
    public async Task CreateHospitalUserAsync_WhenEmailAlreadyExists_ThrowsConflictException()
    {
        await using var dbContext = CreateDbContext();
        var hospitalId = Guid.NewGuid();

        dbContext.Hospitals.Add(new Hospital
        {
            Id = hospitalId,
            Name = "Central Hospital",
            IsActive = true
        });

        dbContext.Users.Add(new User
        {
            Username = "existing.user",
            NormalizedUsername = "EXISTING.USER",
            Email = "doctor.one@test.com",
            NormalizedEmail = "DOCTOR.ONE@TEST.COM",
            PasswordHash = new PasswordHashService().HashPassword("Password@123"),
            FirstName = "Existing",
            LastName = "User",
            HospitalId = hospitalId,
            Status = EntityStatus.Active,
            Roles = [Role.Doctor]
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, Guid.NewGuid(), hospitalId, Role.HospitalAdmin);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await service.CreateHospitalUserAsync(new CreateHospitalUserRequest(
                "new.user",
                "Doctor.One@Test.com",
                "Password@123",
                "New",
                "User",
                null,
                "Nurse",
                null,
                null,
                null)));

        Assert.That(await dbContext.Users.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateHospitalUserAsync_WhenCurrentUserNotHospitalAdmin_ThrowsForbiddenException()
    {
        await using var dbContext = CreateDbContext();
        var hospitalId = Guid.NewGuid();

        await AddHospitalAsync(dbContext, hospitalId);

        var service = CreateService(dbContext, Guid.NewGuid(), hospitalId, Role.Doctor);

        Assert.ThrowsAsync<ForbiddenException>(async () =>
            await service.CreateHospitalUserAsync(CreateValidRequest()));
    }

    [Test]
    public async Task CreateHospitalUserAsync_WhenHospitalDoesNotExist_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var hospitalId = Guid.NewGuid();
        var service = CreateService(dbContext, Guid.NewGuid(), hospitalId, Role.HospitalAdmin);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.CreateHospitalUserAsync(CreateValidRequest()));
    }

    [Test]
    public async Task CreateHospitalUserAsync_WhenHospitalInactive_ThrowsBusinessRuleException()
    {
        await using var dbContext = CreateDbContext();
        var hospitalId = Guid.NewGuid();

        await AddHospitalAsync(dbContext, hospitalId, isActive: false);

        var service = CreateService(dbContext, Guid.NewGuid(), hospitalId, Role.HospitalAdmin);

        Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.CreateHospitalUserAsync(CreateValidRequest()));
    }

    [Test]
    public async Task CreateHospitalUserAsync_WhenUsernameExists_ThrowsConflictException()
    {
        await using var dbContext = CreateDbContext();
        var hospitalId = Guid.NewGuid();

        await AddHospitalAsync(dbContext, hospitalId);

        dbContext.Users.Add(new User
        {
            Username = "existing.user",
            NormalizedUsername = "EXISTING.USER",
            Email = "existing.user@test.com",
            NormalizedEmail = "EXISTING.USER@TEST.COM",
            PasswordHash = new PasswordHashService().HashPassword("Password@123"),
            FirstName = "Existing",
            LastName = "User",
            HospitalId = hospitalId,
            Status = EntityStatus.Active,
            Roles = [Role.Doctor]
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, Guid.NewGuid(), hospitalId, Role.HospitalAdmin);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await service.CreateHospitalUserAsync(CreateValidRequest(
                username: "Existing.User",
                email: "new.user@test.com")));

        Assert.That(await dbContext.Users.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateHospitalUserAsync_WhenHospitalClaimMissing_ThrowsForbiddenException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, Guid.NewGuid(), hospitalId: null, Role.HospitalAdmin);

        Assert.ThrowsAsync<ForbiddenException>(async () =>
            await service.CreateHospitalUserAsync(CreateValidRequest()));
    }

    private static UsersService CreateService(
        VaccineTrackerDbContext dbContext,
        Guid currentUserId,
        Guid? hospitalId,
        Role role)
    {
        return new UsersService(
            dbContext,
            new PasswordHashService(),
            new TestCurrentUser(currentUserId, hospitalId, role.ToString()),
            NullLogger<UsersService>.Instance);
    }

    private static CreateHospitalUserRequest CreateValidRequest(
        string username = "doctor.one",
        string email = "doctor.one@test.com")
    {
        return new CreateHospitalUserRequest(
            username,
            email,
            "Password@123",
            "Doctor",
            "One",
            null,
            "Doctor",
            "Male",
            "0771234567",
            "EMP-001");
    }

    private static async Task AddHospitalAsync(
        VaccineTrackerDbContext dbContext,
        Guid hospitalId,
        bool isActive = true)
    {
        dbContext.Hospitals.Add(new Hospital
        {
            Id = hospitalId,
            Name = "Central Hospital",
            IsActive = isActive
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
        public TestCurrentUser(Guid userId, Guid? hospitalId, string role)
        {
            UserId = userId;
            HospitalId = hospitalId;
            Role = role;
        }

        public Guid UserId { get; }

        public Guid? HospitalId { get; }

        public string Email => "admin@test.com";

        public string Role { get; }
    }
}
