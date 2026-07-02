using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccineScheduleItems;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class VaccineScheduleItemsService : IVaccineScheduleItemsService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<VaccineScheduleItemsService> _logger;

    public VaccineScheduleItemsService(
        VaccineTrackerDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<VaccineScheduleItemsService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResponse<VaccineScheduleItemResponse>> GetScheduleItemsAsync(
        GetVaccineScheduleItemsRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.VaccineScheduleItems
            .AsNoTracking()
            .Where(item => !item.IsDeleted);

        if (request.VaccineTypeId.HasValue)
        {
            query = query.Where(item =>
                item.VaccineTypeId == request.VaccineTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TargetGroup))
        {
            if (!TryParseTargetGroup(request.TargetGroup, out var targetGroup))
            {
                throw new ValidationException(
                    $"Target group '{request.TargetGroup}' is invalid.");
            }

            query = query.Where(item => item.TargetGroup == targetGroup);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(item =>
                item.VaccineType.Name.Contains(search) ||
                item.VaccineType.Code.Contains(search) ||
                (item.Description != null && item.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!TryParseEntityStatus(request.Status, out var status))
            {
                throw new ValidationException(
                    $"Status '{request.Status}' is invalid.");
            }

            query = query.Where(item => item.Status == status);
        }
        else
        {
            query = query.Where(item => item.Status == EntityStatus.Active);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(item => item.VaccineType.Name)
            .ThenBy(item => item.TargetGroup)
            .ThenBy(item => item.DoseNumber)
            .Select(item => new VaccineScheduleItemResponse(
                item.Id,
                item.VaccineTypeId,
                item.VaccineType.Name,
                item.VaccineType.Code,
                item.TargetGroup.ToString(),
                item.DueAgeInDays,
                item.MinimumAgeInYears,
                item.RepeatIntervalInDays,
                item.DoseNumber,
                item.Description,
                item.Status.ToString(),
                item.CreatedAt,
                item.UpdatedAt))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<VaccineScheduleItemResponse>(
            items,
            pageNumber,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task<VaccineScheduleItemResponse> GetScheduleItemAsync(
        Guid scheduleItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.VaccineScheduleItems
            .AsNoTracking()
            .Include(item => item.VaccineType)
            .FirstOrDefaultAsync(
                item => item.Id == scheduleItemId &&
                    !item.IsDeleted,
                cancellationToken);

        if (item is null)
        {
            throw new NotFoundException("Vaccine schedule item", scheduleItemId);
        }

        return ToResponse(item);
    }

    public async Task<VaccineScheduleItemResponse> CreateScheduleItemAsync(
        CreateVaccineScheduleItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseTargetGroup(request.TargetGroup, out var targetGroup))
        {
            throw new ValidationException(
                $"Target group '{request.TargetGroup}' is invalid.");
        }

        ValidateScheduleTiming(request);

        await EnsureVaccineTypeCanBeUsedAsync(
            request.VaccineTypeId,
            cancellationToken);

        await EnsureDoseIsUniqueAsync(
            request.VaccineTypeId,
            targetGroup,
            request.DoseNumber,
            ignoredScheduleItemId: null,
            cancellationToken);

        var item = new VaccineScheduleItem
        {
            VaccineTypeId = request.VaccineTypeId,
            TargetGroup = targetGroup,
            DueAgeInDays = request.DueAgeInDays,
            MinimumAgeInYears = request.MinimumAgeInYears,
            RepeatIntervalInDays = request.RepeatIntervalInDays,
            DoseNumber = request.DoseNumber,
            Description = TrimOrNull(request.Description),
            Status = EntityStatus.Active,
            CreatedBy = _currentUser.UserId.ToString()
        };

        _dbContext.VaccineScheduleItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccine schedule item {ScheduleItemId} created.",
            item.Id);

        return await GetScheduleItemAsync(item.Id, cancellationToken);
    }

    public async Task<VaccineScheduleItemResponse> UpdateScheduleItemAsync(
        Guid scheduleItemId,
        UpdateVaccineScheduleItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await GetTrackedScheduleItemAsync(
            scheduleItemId,
            cancellationToken);

        if (!TryParseTargetGroup(request.TargetGroup, out var targetGroup))
        {
            throw new ValidationException(
                $"Target group '{request.TargetGroup}' is invalid.");
        }

        ValidateScheduleTiming(request);

        await EnsureVaccineTypeCanBeUsedAsync(
            request.VaccineTypeId,
            cancellationToken);

        await EnsureDoseIsUniqueAsync(
            request.VaccineTypeId,
            targetGroup,
            request.DoseNumber,
            ignoredScheduleItemId: scheduleItemId,
            cancellationToken);

        item.VaccineTypeId = request.VaccineTypeId;
        item.TargetGroup = targetGroup;
        item.DueAgeInDays = request.DueAgeInDays;
        item.MinimumAgeInYears = request.MinimumAgeInYears;
        item.RepeatIntervalInDays = request.RepeatIntervalInDays;
        item.DoseNumber = request.DoseNumber;
        item.Description = TrimOrNull(request.Description);
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccine schedule item {ScheduleItemId} updated.",
            item.Id);

        return await GetScheduleItemAsync(item.Id, cancellationToken);
    }

    public async Task<VaccineScheduleItemResponse> ActivateScheduleItemAsync(
        Guid scheduleItemId,
        CancellationToken cancellationToken = default)
    {
        return await SetScheduleItemStatusAsync(
            scheduleItemId,
            EntityStatus.Active,
            cancellationToken);
    }

    public async Task<VaccineScheduleItemResponse> DeactivateScheduleItemAsync(
        Guid scheduleItemId,
        CancellationToken cancellationToken = default)
    {
        return await SetScheduleItemStatusAsync(
            scheduleItemId,
            EntityStatus.Inactive,
            cancellationToken);
    }

    private async Task<VaccineScheduleItemResponse> SetScheduleItemStatusAsync(
        Guid scheduleItemId,
        EntityStatus status,
        CancellationToken cancellationToken)
    {
        var item = await GetTrackedScheduleItemAsync(
            scheduleItemId,
            cancellationToken);

        if (item.Status == status)
        {
            throw new BusinessRuleException(
                $"Vaccine schedule item is already {status}.");
        }

        item.Status = status;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccine schedule item {ScheduleItemId} status changed to {Status}.",
            item.Id,
            item.Status);

        return await GetScheduleItemAsync(item.Id, cancellationToken);
    }

    private async Task<VaccineScheduleItem> GetTrackedScheduleItemAsync(
        Guid scheduleItemId,
        CancellationToken cancellationToken)
    {
        var item = await _dbContext.VaccineScheduleItems
            .FirstOrDefaultAsync(
                item => item.Id == scheduleItemId &&
                    !item.IsDeleted,
                cancellationToken);

        if (item is null)
        {
            throw new NotFoundException("Vaccine schedule item", scheduleItemId);
        }

        return item;
    }

    private async Task EnsureVaccineTypeCanBeUsedAsync(
        Guid vaccineTypeId,
        CancellationToken cancellationToken)
    {
        var vaccineType = await _dbContext.VaccineTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                vaccineType => vaccineType.Id == vaccineTypeId &&
                    !vaccineType.IsDeleted,
                cancellationToken);

        if (vaccineType is null)
        {
            throw new NotFoundException("Vaccine type", vaccineTypeId);
        }

        if (vaccineType.Status != EntityStatus.Active)
        {
            throw new BusinessRuleException(
                "Vaccine schedule item cannot use an inactive vaccine type.");
        }
    }

    private async Task EnsureDoseIsUniqueAsync(
        Guid vaccineTypeId,
        VaccineTargetGroup targetGroup,
        int doseNumber,
        Guid? ignoredScheduleItemId,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.VaccineScheduleItems
            .AnyAsync(
                item =>
                    !item.IsDeleted &&
                    item.VaccineTypeId == vaccineTypeId &&
                    item.TargetGroup == targetGroup &&
                    item.DoseNumber == doseNumber &&
                    (!ignoredScheduleItemId.HasValue ||
                        item.Id != ignoredScheduleItemId.Value),
                cancellationToken);

        if (exists)
        {
            throw new ConflictException(
                $"Dose {doseNumber} already exists for this vaccine type and target group.");
        }
    }

    private static void ValidateScheduleTiming(CreateVaccineScheduleItemRequest request)
    {
        ValidateScheduleTiming(
            request.DueAgeInDays,
            request.MinimumAgeInYears,
            request.RepeatIntervalInDays,
            request.DoseNumber);
    }

    private static void ValidateScheduleTiming(UpdateVaccineScheduleItemRequest request)
    {
        ValidateScheduleTiming(
            request.DueAgeInDays,
            request.MinimumAgeInYears,
            request.RepeatIntervalInDays,
            request.DoseNumber);
    }

    private static void ValidateScheduleTiming(
        int? dueAgeInDays,
        int? minimumAgeInYears,
        int? repeatIntervalInDays,
        int doseNumber)
    {
        if (doseNumber <= 0)
        {
            throw new ValidationException("Dose number must be greater than zero.");
        }

        if (dueAgeInDays is < 0)
        {
            throw new ValidationException("Due age in days cannot be negative.");
        }

        if (minimumAgeInYears is < 0)
        {
            throw new ValidationException("Minimum age in years cannot be negative.");
        }

        if (repeatIntervalInDays is < 0)
        {
            throw new ValidationException("Repeat interval in days cannot be negative.");
        }

        if (!dueAgeInDays.HasValue &&
            !minimumAgeInYears.HasValue &&
            !repeatIntervalInDays.HasValue)
        {
            throw new ValidationException(
                "At least one schedule timing value is required.");
        }
    }

    private static bool TryParseTargetGroup(string targetGroup, out VaccineTargetGroup value)
    {
        return Enum.TryParse(targetGroup, ignoreCase: true, out value) &&
            Enum.IsDefined(value);
    }

    private static bool TryParseEntityStatus(string status, out EntityStatus value)
    {
        return Enum.TryParse(status, ignoreCase: true, out value) &&
            Enum.IsDefined(value);
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static VaccineScheduleItemResponse ToResponse(VaccineScheduleItem item)
    {
        return new VaccineScheduleItemResponse(
            item.Id,
            item.VaccineTypeId,
            item.VaccineType.Name,
            item.VaccineType.Code,
            item.TargetGroup.ToString(),
            item.DueAgeInDays,
            item.MinimumAgeInYears,
            item.RepeatIntervalInDays,
            item.DoseNumber,
            item.Description,
            item.Status.ToString(),
            item.CreatedAt,
            item.UpdatedAt);
    }
}
