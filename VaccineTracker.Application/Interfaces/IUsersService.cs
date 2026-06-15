using VaccineTracker.Application.Users;
using VaccineTracker.Contracts.Users;

namespace VaccineTracker.Application.Interfaces;

public interface IUsersService
{
    Task<UserOperationResult<UserResponse>> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserOperationResult<UserResponse>> CreateHospitalUserAsync(
        CreateHospitalUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserOperationResult<UserResponse>> AssignRoleAsync(
        Guid userId,
        AssignUserRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<UserOperationResult<UserResponse>> ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserOperationResult<UserResponse>> DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
