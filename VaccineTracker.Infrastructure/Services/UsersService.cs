using Microsoft.EntityFrameworkCore;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Application.Users;
using VaccineTracker.Contracts.Users;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Authentication;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class UsersService : IUsersService
{
    private static readonly Role[] HospitalAdminAssignableRoles =
    [
        Role.Doctor,
        Role.Nurse,
        Role.Staff,
        Role.Patient
    ];

    private static readonly Role[] PlatformAdminAssignableHospitalRoles =
    [
        Role.HospitalAdmin,
        Role.Doctor,
        Role.Nurse,
        Role.Staff,
        Role.Patient
    ];

    private readonly VaccineTrackerDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ICurrentUser _currentUser;

    public UsersService(
        VaccineTrackerDbContext dbContext,
        IPasswordHashService passwordHashService,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
        _currentUser = currentUser;
    }

    public async Task<UserOperationResult<UserResponse>> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetManageableUserAsync(userId, cancellationToken);

        return user.Status == UserOperationStatus.Success && user.Value is not null
            ? new UserOperationResult<UserResponse>(UserOperationStatus.Success, ToResponse(user.Value))
            : new UserOperationResult<UserResponse>(user.Status);
    }

    public async Task<UserOperationResult<UserResponse>> CreateHospitalUserAsync(
        CreateHospitalUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseRole(request.Role, out var role))
        {
            return new UserOperationResult<UserResponse>(UserOperationStatus.InvalidRole);
        }

        if (!CanAssignRole(role))
        {
            return new UserOperationResult<UserResponse>(UserOperationStatus.Forbidden);
        }

        if (!TryParseOptionalGender(request.Gender, out var gender))
        {
            return new UserOperationResult<UserResponse>(UserOperationStatus.InvalidGender);
        }

        var hospitalId = ResolveHospitalId(request.HospitalId);
        if (!hospitalId.HasValue)
        {
            return new UserOperationResult<UserResponse>(UserOperationStatus.InvalidHospital);
        }

        if (!CanAccessHospital(hospitalId.Value))
        {
            return new UserOperationResult<UserResponse>(UserOperationStatus.Forbidden);
        }

        var hospitalExists = await _dbContext.Hospitals
            .AnyAsync(hospital => hospital.Id == hospitalId.Value && !hospital.IsDeleted && hospital.IsActive, cancellationToken);

        if (!hospitalExists)
        {
            return new UserOperationResult<UserResponse>(UserOperationStatus.InvalidHospital);
        }

        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var userExists = await _dbContext.Users
            .AnyAsync(user =>
                !user.IsDeleted &&
                (user.Username == username || user.Email == email),
                cancellationToken);

        if (userExists)
        {
            return new UserOperationResult<UserResponse>(UserOperationStatus.Conflict);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = _passwordHashService.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Gender = gender,
            HospitalId = hospitalId,
            PhoneNumber = request.PhoneNumber,
            EmployeeId = request.EmployeeId,
            Status = EntityStatus.Active,
            Roles = [role],
            CreatedAt = now,
            CreatedBy = _currentUser.UserId.ToString()
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserOperationResult<UserResponse>(UserOperationStatus.Success, ToResponse(user));
    }

    public async Task<UserOperationResult<UserResponse>> AssignRoleAsync(
        Guid userId,
        AssignUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseRole(request.Role, out var role))
        {
            return new UserOperationResult<UserResponse>(UserOperationStatus.InvalidRole);
        }

        if (!CanAssignRole(role))
        {
            return new UserOperationResult<UserResponse>(UserOperationStatus.Forbidden);
        }

        var user = await GetManageableUserAsync(userId, cancellationToken);
        if (user.Status == UserOperationStatus.NotFound || user.Value is null)
        {
            return new UserOperationResult<UserResponse>(user.Status);
        }

        if (user.Status == UserOperationStatus.Forbidden)
        {
            return new UserOperationResult<UserResponse>(user.Status);
        }

        user.Value.Roles = [role];
        user.Value.UpdatedAt = DateTime.UtcNow;
        user.Value.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserOperationResult<UserResponse>(UserOperationStatus.Success, ToResponse(user.Value));
    }

    public async Task<UserOperationResult<UserResponse>> ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await SetUserStatusAsync(userId, EntityStatus.Active, cancellationToken);
    }

    public async Task<UserOperationResult<UserResponse>> DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == _currentUser.UserId)
        {
            return new UserOperationResult<UserResponse>(UserOperationStatus.Forbidden);
        }

        return await SetUserStatusAsync(userId, EntityStatus.Inactive, cancellationToken);
    }

    private async Task<UserOperationResult<User>> GetManageableUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == userId && !user.IsDeleted, cancellationToken);

        if (user is null)
        {
            return new UserOperationResult<User>(UserOperationStatus.NotFound);
        }

        return user.HospitalId.HasValue && CanAccessHospital(user.HospitalId.Value)
            ? new UserOperationResult<User>(UserOperationStatus.Success, user)
            : new UserOperationResult<User>(UserOperationStatus.Forbidden);
    }

    private async Task<UserOperationResult<UserResponse>> SetUserStatusAsync(
        Guid userId,
        EntityStatus status,
        CancellationToken cancellationToken)
    {
        var user = await GetManageableUserAsync(userId, cancellationToken);
        if (user.Status != UserOperationStatus.Success || user.Value is null)
        {
            return new UserOperationResult<UserResponse>(user.Status);
        }

        user.Value.Status = status;
        user.Value.UpdatedAt = DateTime.UtcNow;
        user.Value.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserOperationResult<UserResponse>(UserOperationStatus.Success, ToResponse(user.Value));
    }

    private Guid? ResolveHospitalId(Guid? requestedHospitalId)
    {
        return IsPlatformAdmin()
            ? requestedHospitalId
            : _currentUser.HospitalId;
    }

    private bool CanAccessHospital(Guid hospitalId)
    {
        return IsPlatformAdmin() ||
            (_currentUser.HospitalId.HasValue && _currentUser.HospitalId.Value == hospitalId);
    }

    private bool CanAssignRole(Role role)
    {
        return IsPlatformAdmin()
            ? PlatformAdminAssignableHospitalRoles.Contains(role)
            : HospitalAdminAssignableRoles.Contains(role);
    }

    private bool IsPlatformAdmin()
    {
        return string.Equals(_currentUser.Role, Role.PlatformAdmin.ToString(), StringComparison.Ordinal);
    }

    private static bool TryParseRole(string role, out Role value)
    {
        return Enum.TryParse(role, ignoreCase: true, out value) &&
            Enum.IsDefined(value);
    }

    private static bool TryParseOptionalGender(string? gender, out Gender? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(gender))
        {
            return true;
        }

        if (!Enum.TryParse<Gender>(gender, ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.HospitalId,
            user.Roles.Select(role => role.ToString()).ToArray(),
            user.Gender?.ToString(),
            user.PhoneNumber,
            user.Status.ToString(),
            user.EmployeeId,
            user.CreatedAt,
            user.UpdatedAt);
    }
}
