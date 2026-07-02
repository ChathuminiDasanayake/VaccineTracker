using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccinationRecords;

namespace VaccineTracker.Application.Interfaces;

public interface IVaccinationRecordsService
{
    Task<PagedResponse<VaccinationRecordResponse>> GetRecordsAsync(
        GetVaccinationRecordsRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccinationRecordResponse> GetRecordAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);

    Task<VaccinationRecordResponse> CreateRecordAsync(
        CreateVaccinationRecordRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccinationRecordResponse> UpdateRecordAsync(
        Guid recordId,
        UpdateVaccinationRecordRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccinationRecordResponse> CancelRecordAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);

    Task<VaccinationRecordResponse> MarkRecordEnteredInErrorAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);
}
