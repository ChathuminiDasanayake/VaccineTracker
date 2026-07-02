using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VaccineTracker.API.Authentication;
using Microsoft.OpenApi.Models;
using Serilog;
using VaccineTracker.API.Authorization;
using VaccineTracker.API.RateLimiting;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Authentication;
using VaccineTracker.Infrastructure.Persistence;
using VaccineTracker.Infrastructure.Services;
using VaccineTracker.API.Middleware;
using VaccineTracker.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<VaccineTrackerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
    string.IsNullOrWhiteSpace(jwtSettings.Audience) ||
    string.IsNullOrWhiteSpace(jwtSettings.Secret) ||
    Encoding.UTF8.GetByteCount(jwtSettings.Secret) < 32 ||
    jwtSettings.ExpiryMinutes <= 0)
{
    throw new InvalidOperationException(
        "Jwt settings must include Issuer, Audience, a secret of at least 32 bytes, and a positive expiry.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(AuthorizationPolicies.PlatformAdmin, policy =>
        policy.RequireRole(Role.PlatformAdmin.ToString()));

    options.AddPolicy(AuthorizationPolicies.HospitalAdmin, policy =>
        policy.RequireRole(
            Role.PlatformAdmin.ToString(),
            Role.HospitalAdmin.ToString()));

    options.AddPolicy(AuthorizationPolicies.HospitalStaff, policy =>
        policy.RequireRole(
            Role.HospitalAdmin.ToString(),
            Role.Doctor.ToString(),
            Role.Nurse.ToString(),
            Role.Staff.ToString()));

    options.AddPolicy(AuthorizationPolicies.ViewPatientSensitiveData, policy =>
        policy.RequireRole(
            Role.HospitalAdmin.ToString(),
            Role.Doctor.ToString(),
            Role.Nurse.ToString()));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.Login, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IHospitalsService, HospitalsService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IPatientsService, PatientsService>();
builder.Services.AddScoped<IVaccinesService, VaccinesService>();
builder.Services.AddScoped<IVaccineManufacturersService, VaccineManufacturersService>();
builder.Services.AddScoped<IVaccineProductsService, VaccineProductsService>();
builder.Services.AddScoped<IVaccineScheduleItemsService, VaccineScheduleItemsService>();
builder.Services.AddScoped<IVaccinationRecordsService, VaccinationRecordsService>();
builder.Services.AddScoped<IRequestContext, RequestContext>();
builder.Services.AddScoped<ILoginAuditService, LoginAuditService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set(
            "CorrelationId",
            httpContext.TraceIdentifier);

        diagnosticContext.Set(
            "UserId",
            httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        diagnosticContext.Set(
            "ClientIp",
            httpContext.Connection.RemoteIpAddress?.ToString());
    };
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
