using VaccineTracker.Contracts.Patients;

namespace VaccineTracker.Application.Interfaces;

public interface IPatientsService
{
    Task<PatientResponse> GetPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PatientResponse> CreatePatientAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default);
}
