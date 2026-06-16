using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Contracts.Auth;
using VaccineTracker.Infrastructure.Authentication;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class AuthServiceTests
{
    [Test]
    public async Task LoginAsync_WithValidCredentials_ReturnsAccessToken()
    {
        await using var dbContext = CreateDbContext();
        var passwordHashService = new PasswordHashService();
        var hospitalId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        dbContext.Hospitals.Add(new Hospital
        {
            Id = hospitalId,
            Name = "Central Hospital",
            IsActive = true
        });

        dbContext.Users.Add(new User
        {
            Id = userId,
            Username = "doctor.one",
            NormalizedUsername = "DOCTOR.ONE",
            Email = "doctor.one@test.com",
            NormalizedEmail = "DOCTOR.1@TEST.COM",
            PasswordHash = passwordHashService.HashPassword("Password@123"),
            FirstName = "Doctor",
            LastName = "One",
            HospitalId = hospitalId,
            Status = EntityStatus.Active,
            Roles = [Role.Doctor]
        });

        await dbContext.SaveChangesAsync();

        var service = new AuthService(
            dbContext,
            passwordHashService,
            Options.Create(CreateJwtSettings()));

        var response = await service.LoginAsync(new LoginRequest(" doctor.one ", "Password@123"));

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.AccessToken, Is.Not.Empty);
            Assert.That(response.UserId, Is.EqualTo(userId));
            Assert.That(response.Username, Is.EqualTo("doctor.one"));
            Assert.That(response.HospitalId, Is.EqualTo(hospitalId));
            Assert.That(response.Roles, Is.EquivalentTo(new[] { "Doctor" }));
        });

        var updatedUser = await dbContext.Users.SingleAsync(user => user.Id == userId);
        Assert.That(updatedUser.LastLoginAt, Is.Not.Null);
    }

    private static VaccineTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VaccineTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaccineTrackerDbContext(options);
    }

    private static JwtSettings CreateJwtSettings()
    {
        return new JwtSettings
        {
            Issuer = "VaccineTracker.Tests",
            Audience = "VaccineTracker.Tests",
            Secret = "test-secret-key-with-at-least-32-characters",
            ExpiryMinutes = 60
        };
    }
}
