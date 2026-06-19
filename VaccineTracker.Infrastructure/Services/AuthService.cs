using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VaccineTracker.Application.Authentication;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Auth;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Authentication;
using VaccineTracker.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace VaccineTracker.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly ILoginAuditService _loginAuditService;

    public AuthService(
        VaccineTrackerDbContext dbContext,
        IPasswordHashService passwordHashService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger,
        ILoginAuditService loginAuditService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _loginAuditService = loginAuditService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login failed because username or password was empty.");
            await _loginAuditService.RecordLoginAsync(
        userId: null,
        username: request.Username ?? string.Empty,
        isSuccessful: false,
        failureReason: "Empty credentials",
        cancellationToken);
            return null;

        }

        var username = request.Username.Trim();
        var normalizedUsername = username.ToUpperInvariant();
        _logger.LogInformation("Login attempt started for username {Username}.", username);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.NormalizedUsername == normalizedUsername &&
                    user.Status == EntityStatus.Active &&
                    !user.IsDeleted,
                cancellationToken);

        if (user is null || !_passwordHashService.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for username {Username}.", username);
            await _loginAuditService.RecordLoginAsync(
        userId: user?.Id,
        username: username,
        isSuccessful: false,
        failureReason: "Invalid credentials",
        cancellationToken);

            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _loginAuditService.RecordLoginAsync(
    userId: user.Id,
    username: user.Username,
    isSuccessful: true,
    failureReason: null,
    cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = user.Roles.Select(role => role.ToString()).ToArray();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
        var token = GenerateToken(user.Id, user.Username, user.Email, roles, user.HospitalId, expiresAtUtc);

        _logger.LogInformation("Login succeeded for user {UserId}.", user.Id);

        return new LoginResponse(
            token,
            expiresAtUtc,
            user.Id,
            user.Username,
            roles,
            user.HospitalId);
    }

    private string GenerateToken(
        Guid userId,
        string username,
        string email,
        IReadOnlyCollection<string> roles,
        Guid? hospitalId,
        DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtClaimNames.UserId, userId.ToString())
        };

        if (hospitalId.HasValue)
        {
            claims.Add(new Claim(JwtClaimNames.HospitalId, hospitalId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim(JwtClaimNames.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
