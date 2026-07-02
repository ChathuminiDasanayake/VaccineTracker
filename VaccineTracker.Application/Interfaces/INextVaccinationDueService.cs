using VaccineTracker.Contracts.VaccinationRecords;

namespace VaccineTracker.Application.Interfaces;

public interface INextVaccinationDueService
{
    Task<NextVaccinationDueResponse?> GetNextDueForPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<NextVaccinationDueResponse?> GetNextDueAfterRecordAsync(
        Guid vaccinationRecordId,
        CancellationToken cancellationToken = default);
}
