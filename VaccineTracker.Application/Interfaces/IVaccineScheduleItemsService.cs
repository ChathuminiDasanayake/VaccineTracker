using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccineScheduleItems;

namespace VaccineTracker.Application.Interfaces;

public interface IVaccineScheduleItemsService
{
    Task<PagedResponse<VaccineScheduleItemResponse>> GetScheduleItemsAsync(
        GetVaccineScheduleItemsRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineScheduleItemResponse> GetScheduleItemAsync(
        Guid scheduleItemId,
        CancellationToken cancellationToken = default);

    Task<VaccineScheduleItemResponse> CreateScheduleItemAsync(
        CreateVaccineScheduleItemRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineScheduleItemResponse> UpdateScheduleItemAsync(
        Guid scheduleItemId,
        UpdateVaccineScheduleItemRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineScheduleItemResponse> ActivateScheduleItemAsync(
        Guid scheduleItemId,
        CancellationToken cancellationToken = default);

    Task<VaccineScheduleItemResponse> DeactivateScheduleItemAsync(
        Guid scheduleItemId,
        CancellationToken cancellationToken = default);
}
