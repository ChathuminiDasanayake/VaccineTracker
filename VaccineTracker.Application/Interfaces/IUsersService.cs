using VaccineTracker.Contracts.Users;

namespace VaccineTracker.Application.Interfaces;

public interface IUsersService
{
    Task<UserResponse> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserResponse> CreateHospitalUserAsync(
        CreateHospitalUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserResponse> AssignRoleAsync(
        Guid userId,
        AssignUserRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<UserResponse> ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserResponse> DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
