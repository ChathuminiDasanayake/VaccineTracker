using Microsoft.EntityFrameworkCore;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Users;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Authentication;
using VaccineTracker.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using VaccineTracker.Application.Exceptions;

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
    private readonly ILogger<UsersService> _logger;

    public UsersService(
        VaccineTrackerDbContext dbContext,
        IPasswordHashService passwordHashService,
        ICurrentUser currentUser,
        ILogger<UsersService> logger)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<UserResponse> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user {UserId}.", userId);

        var user = await GetManageableUserAsync(userId, cancellationToken);

        return ToResponse(user);
    }

    public async Task<UserResponse> CreateHospitalUserAsync(
        CreateHospitalUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseRole(request.Role, out var role))
        {
            throw new ValidationException(
    $"Role '{request.Role}' is invalid.");
        }

        if (!CanAssignRole(role))
        {
            throw new ForbiddenException(
    $"You cannot assign the requested role '{role}'.");
        }

        if (!TryParseOptionalGender(request.Gender, out var gender))
        {
            throw new ValidationException(
    $"Gender '{request.Gender}' is invalid.");
        }

        if (!CanManageHospitalUsers())
        {
            throw new ForbiddenException("You cannot manage hospital users.");
        }

        var hospitalId = ResolveHospitalId(request.HospitalId);
        if (!IsPlatformAdmin() && !hospitalId.HasValue)
        {
            throw new ForbiddenException(
                "Your account is not associated with a hospital.");
        }

        if (!hospitalId.HasValue)
        {
            throw new ValidationException("Hospital ID is required.");
        }

        if (!CanAccessHospital(hospitalId.Value))
        {
            throw new ForbiddenException(
                $"You cannot access hospital '{hospitalId.Value}'.");
        }

        _logger.LogInformation("Creating user with username {Username} for hospital {HospitalId}.", request.Username, hospitalId.Value);

        var hospital = await _dbContext.Hospitals
            .AsNoTracking()
            .FirstOrDefaultAsync(hospital => hospital.Id == hospitalId.Value && !hospital.IsDeleted, cancellationToken);

        if (hospital is null)
        {
            throw new NotFoundException("Hospital", hospitalId.Value);
        }

        if (!hospital.IsActive)
        {
            throw new BusinessRuleException(
                "User creation failed because the hospital is inactive.");
        }

        var username = request.Username.Trim();
        var normalizedUsername = Normalize(username);
        var email = request.Email.Trim();
        var normalizedEmail = Normalize(email);
        var userExists = await _dbContext.Users
            .AnyAsync(user =>
                !user.IsDeleted &&
                (user.NormalizedUsername == normalizedUsername || user.NormalizedEmail == normalizedEmail),
                cancellationToken);

        if (userExists)
        {
            throw new ConflictException(
                "A user with this username or email already exists.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            Email = email,
            NormalizedEmail = normalizedEmail,
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

        _logger.LogInformation("User {UserId} created successfully.", user.Id);

        return ToResponse(user);
    }

    public async Task<UserResponse> AssignRoleAsync(
        Guid userId,
        AssignUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseRole(request.Role, out var role))
        {
            throw new ValidationException(
                $"Role '{request.Role}' is invalid.");
        }

        if (!CanManageHospitalUsers())
        {
            throw new ForbiddenException(
                $"You cannot manage user '{userId}'.");
        }

        if (!CanAssignRole(role))
        {
            throw new ForbiddenException(
                $"You cannot assign the role '{role}'.");
        }

        var user = await GetManageableUserAsync(userId, cancellationToken);

        user.Roles = [role];
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role {Role} assigned to user {UserId}.", role, userId);

        return ToResponse(user);
    }

    public async Task<UserResponse> ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await SetUserStatusAsync(userId, EntityStatus.Active, cancellationToken);
    }

    public async Task<UserResponse> DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == _currentUser.UserId)
        {
            throw new BusinessRuleException(
                "You cannot deactivate your own account.");
        }

        return await SetUserStatusAsync(userId, EntityStatus.Inactive, cancellationToken);
    }

    private async Task<User> GetManageableUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == userId && !user.IsDeleted, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User", userId);
        }

        if (!user.HospitalId.HasValue || !CanAccessHospital(user.HospitalId.Value))
        {
            throw new ForbiddenException(
                $"You cannot access user '{userId}'.");
        }

        return user;
    }

    private async Task<UserResponse> SetUserStatusAsync(
        Guid userId,
        EntityStatus status,
        CancellationToken cancellationToken)
    {
        var user = await GetManageableUserAsync(userId, cancellationToken);

        user.Status = status;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} status changed to {Status}.", userId, status);

        return ToResponse(user);
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

    private bool CanManageHospitalUsers()
    {
        return IsPlatformAdmin() || IsHospitalAdmin();
    }

    private bool IsPlatformAdmin()
    {
        return string.Equals(_currentUser.Role, Role.PlatformAdmin.ToString(), StringComparison.Ordinal);
    }

    private bool IsHospitalAdmin()
    {
        return string.Equals(_currentUser.Role, Role.HospitalAdmin.ToString(), StringComparison.Ordinal);
    }

    private static bool TryParseRole(string role, out Role value)
    {
        return Enum.TryParse(role, ignoreCase: true, out value) &&
            Enum.IsDefined(value);
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
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
