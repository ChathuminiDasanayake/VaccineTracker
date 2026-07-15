using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.Patients;

namespace VaccineTracker.Application.Interfaces;

public interface IPatientsService
{
    Task<PagedResponse<PatientSummaryResponse>> GetPatientsAsync(
        PatientSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientSummaryResponse> GetPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PatientDetailsResponse> GetPatientDetailsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PatientSummaryResponse> CreatePatientAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientSummaryResponse> UpdatePatientAsync(
        Guid patientId,
        UpdatePatientRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientSummaryResponse> UpdatePatientStatusAsync(
        Guid patientId,
        UpdatePatientStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientPortalAccessResponse> LinkPatientPortalAccessAsync(
        Guid patientId,
        LinkPatientPortalAccessRequest request,
        CancellationToken cancellationToken = default);
}
