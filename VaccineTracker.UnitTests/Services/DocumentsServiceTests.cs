using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Documents;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;

namespace VaccineTracker.UnitTests.Services;

public sealed class DocumentsServiceTests
{
    [Test]
    public async Task GetMyDocumentsAsync_ReturnsOnlyLinkedPatientDocuments()
    {
        await using var dbContext = CreateDbContext();
        var patientUserId = Guid.NewGuid();
        var hospital = AddHospital(dbContext);
        var linkedPatient = AddPatient(dbContext, hospital.Id, "PT-LINKED");
        var otherPatient = AddPatient(dbContext, hospital.Id, "PT-OTHER");

        AddDocument(dbContext, hospital.Id, linkedPatient.Id, "linked.pdf");
        AddDocument(dbContext, hospital.Id, otherPatient.Id, "other.pdf");
        AddPortalAccess(dbContext, patientUserId, linkedPatient.Id);
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            userId: patientUserId,
            hospitalId: null,
            role: Role.Patient);

        var result = await service.GetMyDocumentsAsync(
            new GetDocumentsRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items.Single().FileName, Is.EqualTo("linked.pdf"));
        });
    }

    [Test]
    public async Task GetMyDocumentAsync_WhenDocumentBelongsToUnlinkedPatient_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var patientUserId = Guid.NewGuid();
        var hospital = AddHospital(dbContext);
        var linkedPatient = AddPatient(dbContext, hospital.Id, "PT-LINKED");
        var otherPatient = AddPatient(dbContext, hospital.Id, "PT-OTHER");
        var otherDocument = AddDocument(dbContext, hospital.Id, otherPatient.Id, "other.pdf");

        AddPortalAccess(dbContext, patientUserId, linkedPatient.Id);
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            userId: patientUserId,
            hospitalId: null,
            role: Role.Patient);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.GetMyDocumentAsync(otherDocument.Id));
    }

    [Test]
    public async Task DownloadMyDocumentAsync_WhenDocumentLinked_ReturnsStorageFile()
    {
        await using var dbContext = CreateDbContext();
        var patientUserId = Guid.NewGuid();
        var hospital = AddHospital(dbContext);
        var patient = AddPatient(dbContext, hospital.Id, "PT-DOWNLOAD");
        var document = AddDocument(dbContext, hospital.Id, patient.Id, "download.pdf");

        AddPortalAccess(dbContext, patientUserId, patient.Id);
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            userId: patientUserId,
            hospitalId: null,
            role: Role.Patient);

        var result = await service.DownloadMyDocumentAsync(document.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.FileName, Is.EqualTo("download.pdf"));
            Assert.That(result.ContentType, Is.EqualTo("application/pdf"));
        });
    }

    [Test]
    public async Task GetMyDocumentsAsync_WhenCurrentUserIsNotPatient_ThrowsForbiddenException()
    {
        await using var dbContext = CreateDbContext();
        var hospital = AddHospital(dbContext);
        var patient = AddPatient(dbContext, hospital.Id, "PT-STAFF");
        AddDocument(dbContext, hospital.Id, patient.Id, "staff.pdf");
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            userId: Guid.NewGuid(),
            hospitalId: hospital.Id,
            role: Role.Staff);

        Assert.ThrowsAsync<ForbiddenException>(async () =>
            await service.GetMyDocumentsAsync(
                new GetDocumentsRequest()));
    }

    private static DocumentsService CreateService(
        VaccineTrackerDbContext dbContext,
        Guid userId,
        Guid? hospitalId,
        Role role)
    {
        return new DocumentsService(
            dbContext,
            new TestCurrentUser(userId, hospitalId, role),
            new FakeDocumentStorageService(),
            NullLogger<DocumentsService>.Instance);
    }

    private static Hospital AddHospital(
        VaccineTrackerDbContext dbContext)
    {
        var hospital = new Hospital
        {
            Name = $"Hospital {Guid.NewGuid():N}",
            IsActive = true
        };

        dbContext.Hospitals.Add(hospital);

        return hospital;
    }

    private static Patient AddPatient(
        VaccineTrackerDbContext dbContext,
        Guid hospitalId,
        string patientNumber)
    {
        var patient = new Patient
        {
            HospitalId = hospitalId,
            PatientNumber = patientNumber,
            FirstName = "Document",
            LastName = "Patient",
            DateOfBirth = new DateOnly(2020, 1, 1),
            Gender = Gender.Female,
            Status = EntityStatus.Active
        };

        dbContext.Patients.Add(patient);

        return patient;
    }

    private static Document AddDocument(
        VaccineTrackerDbContext dbContext,
        Guid hospitalId,
        Guid patientId,
        string fileName)
    {
        var document = new Document
        {
            HospitalId = hospitalId,
            PatientId = patientId,
            FileName = fileName,
            BlobName = $"{Guid.NewGuid():N}.pdf",
            ContentType = "application/pdf",
            SizeInBytes = 100,
            Type = DocumentType.ConsentForm,
            Status = EntityStatus.Active
        };

        dbContext.Documents.Add(document);

        return document;
    }

    private static void AddPortalAccess(
        VaccineTrackerDbContext dbContext,
        Guid userId,
        Guid patientId)
    {
        dbContext.PatientPortalAccesses.Add(new PatientPortalAccess
        {
            UserId = userId,
            PatientId = patientId
        });
    }

    private static VaccineTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VaccineTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaccineTrackerDbContext(options);
    }

    private sealed class FakeDocumentStorageService : IDocumentStorageService
    {
        public Task<DocumentStorageResult> SaveAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new DocumentStorageResult($"{Guid.NewGuid():N}.pdf"));
        }

        public Task<DocumentStorageFile> OpenReadAsync(
            string blobName,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            Stream stream = new MemoryStream([1, 2, 3]);

            return Task.FromResult(
                new DocumentStorageFile(
                    stream,
                    fileName,
                    contentType));
        }
    }

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

        public string Email => "user@test.com";

        public string Role { get; }
    }
}
