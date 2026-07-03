using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Contracts.Notifications;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class NotificationOutboxServiceTests
{
    [Test]
    public async Task GetPendingAsync_ReturnsDuePendingNotificationsOnly()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedPatientAsync(dbContext);
        var due = AddNotification(
            dbContext,
            seed.Patient,
            NotificationStatus.Pending,
            DateTime.UtcNow.AddMinutes(-5));
        AddNotification(
            dbContext,
            seed.Patient,
            NotificationStatus.Pending,
            DateTime.UtcNow.AddMinutes(30));
        AddNotification(
            dbContext,
            seed.Patient,
            NotificationStatus.Failed,
            DateTime.UtcNow.AddMinutes(-5));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetPendingAsync(
            new GetPendingNotificationsRequest());

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.Single().Id, Is.EqualTo(due.Id));
    }

    [Test]
    public async Task MarkProcessingAsync_WithPendingNotification_MarksProcessingAndIncrementsAttempt()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedPatientAsync(dbContext);
        var notification = AddNotification(
            dbContext,
            seed.Patient,
            NotificationStatus.Pending,
            DateTime.UtcNow.AddMinutes(-5));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.MarkProcessingAsync(notification.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Processing"));
            Assert.That(result.AttemptCount, Is.EqualTo(1));
            Assert.That(result.LastAttemptAtUtc, Is.Not.Null);
        });
    }

    [Test]
    public async Task MarkSentAsync_WithProcessingNotification_MarksSent()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedPatientAsync(dbContext);
        var notification = AddNotification(
            dbContext,
            seed.Patient,
            NotificationStatus.Processing,
            DateTime.UtcNow.AddMinutes(-5));
        notification.AttemptCount = 1;
        notification.LastAttemptAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.MarkSentAsync(notification.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Sent"));
            Assert.That(result.SentAtUtc, Is.Not.Null);
            Assert.That(result.FailureReason, Is.Null);
        });
    }

    [Test]
    public async Task MarkFailedAsync_WithProcessingNotification_MarksFailed()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedPatientAsync(dbContext);
        var notification = AddNotification(
            dbContext,
            seed.Patient,
            NotificationStatus.Processing,
            DateTime.UtcNow.AddMinutes(-5));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.MarkFailedAsync(
            notification.Id,
            new MarkNotificationFailedRequest(" SMTP failed "));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Failed"));
            Assert.That(result.FailureReason, Is.EqualTo("SMTP failed"));
            Assert.That(result.LastAttemptAtUtc, Is.Not.Null);
        });
    }

    [Test]
    public async Task MarkProcessingAsync_WhenNotificationSent_ThrowsBusinessRuleException()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedPatientAsync(dbContext);
        var notification = AddNotification(
            dbContext,
            seed.Patient,
            NotificationStatus.Sent,
            DateTime.UtcNow.AddMinutes(-5));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.MarkProcessingAsync(notification.Id));
    }

    [Test]
    public async Task MarkSentAsync_WhenNotificationNotFound_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.MarkSentAsync(Guid.NewGuid()));
    }

    private static NotificationOutboxService CreateService(
        VaccineTrackerDbContext dbContext)
    {
        return new NotificationOutboxService(
            dbContext,
            NullLogger<NotificationOutboxService>.Instance);
    }

    private static async Task<NotificationSeed> SeedPatientAsync(
        VaccineTrackerDbContext dbContext)
    {
        var hospital = new Hospital
        {
            Name = "Notification Hospital",
            IsActive = true
        };

        var patient = new Patient
        {
            HospitalId = hospital.Id,
            PatientNumber = "PT-NOTIFY",
            FirstName = "Notify",
            LastName = "Patient",
            DateOfBirth = new DateOnly(2025, 1, 1),
            Gender = Gender.Female,
            Email = "patient@test.com",
            Status = EntityStatus.Active
        };

        dbContext.AddRange(hospital, patient);
        await dbContext.SaveChangesAsync();

        return new NotificationSeed(hospital, patient);
    }

    private static NotificationOutbox AddNotification(
        VaccineTrackerDbContext dbContext,
        Patient patient,
        NotificationStatus status,
        DateTime sendAfterUtc)
    {
        var notification = new NotificationOutbox
        {
            PatientId = patient.Id,
            Type = NotificationType.VaccinationReminder,
            Channel = NotificationChannel.Email,
            Recipient = patient.Email!,
            Subject = "Vaccination reminder",
            PayloadJson = "{\"patientNumber\":\"PT-NOTIFY\"}",
            SendAfterUtc = sendAfterUtc,
            Status = status
        };

        dbContext.NotificationOutbox.Add(notification);

        return notification;
    }

    private static VaccineTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VaccineTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaccineTrackerDbContext(options);
    }

    private sealed record NotificationSeed(
        Hospital Hospital,
        Patient Patient);
}
