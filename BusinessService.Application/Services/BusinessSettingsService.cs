using System.Text.Json;
using BusinessService.Application.DTOs;
using BusinessService.Application.DTOs.Settings;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;

namespace BusinessService.Application.Services;

public class BusinessSettingsService : IBusinessSettingsService
{
    private readonly IBusinessSettingsRepository _settingsRepository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IBusinessRepServiceClient _businessRepClient;
    private readonly IUserServiceClient _userServiceClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public BusinessSettingsService(
        IBusinessSettingsRepository settingsRepository,
        IBusinessRepository businessRepository,
        IBusinessRepServiceClient businessRepClient,
        IUserServiceClient userServiceClient)
    {
        _settingsRepository = settingsRepository;
        _businessRepository = businessRepository;
        _businessRepClient = businessRepClient;
        _userServiceClient = userServiceClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    // ========== Business Settings ==========

    public async Task<BusinessSettingsDto> GetBusinessSettingsAsync(Guid businessId)
    {
        _ = await _businessRepository.FindByIdAsync(businessId)
            ?? throw new BusinessNotFoundException($"Business {businessId} not found.");

        var settings = await _settingsRepository.FindBusinessSettingsByBusinessIdAsync(businessId);

        if (settings == null)
        {
            settings = await CreateDefaultBusinessSettingsAsync(businessId);
        }
        else
        {
            if (settings.CheckAndUpdateDndModeExpiry())
            {
                await _settingsRepository.UpdateBusinessSettingsAsync(settings);
            }
        }

        return MapToBusinessSettingsDto(settings);
    }

    public async Task<BusinessSettingsDto> UpdateBusinessSettingsAsync(
        Guid businessId,
        UpdateBusinessSettingsRequest request,
        Guid currentUserId)
    {
        _ = await _businessRepository.FindByIdAsync(businessId)
            ?? throw new BusinessNotFoundException($"Business {businessId} not found.");

        // Authorization: Only parent rep of parent business can modify
        await EnsureUserIsParentRepAsync(businessId, currentUserId);

        var settings = await _settingsRepository.FindBusinessSettingsByBusinessIdAsync(businessId)
                       ?? await CreateDefaultBusinessSettingsAsync(businessId);

        // Update only provided fields
        if (request.ReviewsPrivate.HasValue)
        {
            settings.ReviewsPrivate = request.ReviewsPrivate.Value;
        }

        if (request.DndModeEnabled.HasValue)
        {
            if (request.DndModeEnabled.Value)
            {
                var hours = request.DndModeDurationHours ?? 60;
                if (hours is < 1 or > 60)

                    throw new ArgumentException("DnD mode duration must be between 1 and 60 hours.");
                settings.EnableDndMode(hours);
            }
            else
            {
                settings.DisableDndMode();
            }
        }

        settings.ModifiedByUserId = currentUserId;
        settings.UpdatedAt = DateTime.UtcNow;

        await _settingsRepository.UpdateBusinessSettingsAsync(settings);

        return MapToBusinessSettingsDto(settings);
    }

    public async Task<BusinessSettingsDto> ExtendDndModeAsync(
        Guid businessId,
        int additionalHours,
        Guid currentUserId)
    {
        if (additionalHours is <= 0 or > 168)
            throw new ArgumentException("Additional hours must be between 1 and 168.");

        var settings = await _settingsRepository.FindBusinessSettingsByBusinessIdAsync(businessId)
            ?? throw new BusinessSettingsNotFoundException($"Business settings for business {businessId} not found.");

        if (!settings.DndModeEnabled)
            throw new InvalidOperationException("DnD mode is not currently enabled.");

        // Check if current user is a support user
        var isSupportUser = await _userServiceClient.IsSupportUserAsync(currentUserId);
        if (!isSupportUser)
            throw new UnauthorizedSettingsAccessException("Only support users can extend DnD mode.");

        if (settings.DndModeExpiresAt.HasValue)
        {
            settings.DndModeExpiresAt = settings.DndModeExpiresAt.Value.AddHours(additionalHours);
        }
        else
        {
            settings.DndModeExpiresAt = DateTime.UtcNow.AddHours(additionalHours);
        }

        settings.ModifiedByUserId = currentUserId;
        settings.UpdatedAt = DateTime.UtcNow;
        await _settingsRepository.UpdateBusinessSettingsAsync(settings);

        return MapToBusinessSettingsDto(settings);
    }

    public async Task ProcessExpiredDndModesAsync()
    {
        var expiredSettings = await _settingsRepository.GetExpiredDndModeSettingsAsync();

        foreach (var settings in expiredSettings)
        {
            settings.DisableDndMode();
            await _settingsRepository.UpdateBusinessSettingsAsync(settings);
        }
    }

    // ========== Business Rep Settings ==========

    public async Task<BusinessRepSettingsDto> GetRepSettingsAsync(Guid businessRepId)
    {
        _ = await _businessRepClient.GetBusinessRepByIdAsync(businessRepId)
            ?? throw new BusinessNotFoundException($"Business rep {businessRepId} not found.");

        var settings = await _settingsRepository.FindRepSettingsByRepIdAsync(businessRepId)
                       ?? await CreateDefaultRepSettingsAsync(businessRepId);

        return MapToRepSettingsDto(settings);
    }

    public async Task<BusinessRepSettingsDto> UpdateRepSettingsAsync(
        Guid businessRepId,
        UpdateRepSettingsRequest request,
        Guid currentUserId)
    {
        var businessRep = await _businessRepClient.GetBusinessRepByIdAsync(businessRepId)
            ?? throw new BusinessNotFoundException($"Business rep {businessRepId} not found.");

        // Authorization: User must own this business rep
        if (businessRep.UserId != currentUserId)
            throw new UnauthorizedSettingsAccessException("You can only modify your own settings.");

        var settings = await _settingsRepository.FindRepSettingsByRepIdAsync(businessRepId)
                       ?? await CreateDefaultRepSettingsAsync(businessRepId);


        // Update only provided fields
        if (request.NotificationPreferences != null)
        {
            settings.NotificationPreferences = JsonSerializer.Serialize(request.NotificationPreferences, _jsonOptions);
        }

        if (request.DarkMode.HasValue)
        {
            settings.DarkMode = request.DarkMode.Value;
        }

        if (request.AutoResponseTemplates != null)
        {
            settings.AutoResponseTemplates = JsonSerializer.Serialize(request.AutoResponseTemplates, _jsonOptions);
        }

        if (request.DisabledAccessUsernames != null)
        {
            settings.DisabledAccessUsernames = JsonSerializer.Serialize(request.DisabledAccessUsernames, _jsonOptions);
        }

        settings.ModifiedByUserId = currentUserId;
        settings.UpdatedAt = DateTime.UtcNow;

        await _settingsRepository.UpdateRepSettingsAsync(settings);

        return MapToRepSettingsDto(settings);
    }

    // ========== Combined View ==========

    public async Task<EffectiveSettingsDto> GetEffectiveSettingsAsync(Guid businessRepId)
    {
        var businessRep = await _businessRepClient.GetBusinessRepByIdAsync(businessRepId)
            ?? throw new BusinessNotFoundException($"Business rep {businessRepId} not found.");

        // Get rep settings
        var repSettings = await GetRepSettingsAsync(businessRepId);

        // Get business settings (check parent if this is a branch)
        var business = await _businessRepository.FindByIdAsync(businessRep.BusinessId)
            ?? throw new BusinessNotFoundException($"Business {businessRep.BusinessId} not found.");

        var parentBusinessId = business.ParentBusinessId ?? business.Id;
        var businessSettings = await GetBusinessSettingsAsync(parentBusinessId);

        return new EffectiveSettingsDto(businessSettings, repSettings);
    }

    // ========== Private Helpers ==========

    private async Task<BusinessSettings> CreateDefaultBusinessSettingsAsync(Guid businessId)
    {
        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ReviewsPrivate = false,
            DndModeEnabled = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _settingsRepository.AddBusinessSettingsAsync(settings);
    }

    private async Task<BusinessRepSettings> CreateDefaultRepSettingsAsync(Guid businessRepId)
    {
        var defaultNotifications = new NotificationPreferencesDto(
            Email: true,
            Whatsapp: false,
            InApp: true
        );

        var settings = new BusinessRepSettings
        {
            Id = Guid.NewGuid(),
            BusinessRepId = businessRepId,
            NotificationPreferences = JsonSerializer.Serialize(defaultNotifications, _jsonOptions),
            DarkMode = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _settingsRepository.AddRepSettingsAsync(settings);
    }

    private async Task EnsureUserIsParentRepAsync(Guid businessId, Guid currentUserId)
    {
        var business = await _businessRepository.FindByIdAsync(businessId)
            ?? throw new BusinessNotFoundException($"Business {businessId} not found.");

        // Get parent business ID (if this is a branch, go to parent)
        var parentBusinessId = business.ParentBusinessId ?? business.Id;

        // Get the parent rep (first business rep created for parent business)
        var parentRep = await _businessRepClient.GetParentRepByBusinessIdAsync(parentBusinessId)
            ?? throw new BusinessSettingsNotFoundException("Parent business rep not found.");

        if (parentRep.UserId != currentUserId)
            throw new UnauthorizedSettingsAccessException("Only the parent business representative can modify business settings.");
    }

    private BusinessSettingsDto MapToBusinessSettingsDto(BusinessSettings settings)
    {
        return new BusinessSettingsDto(
            settings.Id,
            settings.BusinessId,
            settings.ReviewsPrivate,
            settings.DndModeEnabled,
            settings.DndModeEnabledAt,
            settings.DndModeExpiresAt,
            settings.CreatedAt,
            settings.UpdatedAt,
            settings.ModifiedByUserId
        );
    }

    private BusinessRepSettingsDto MapToRepSettingsDto(BusinessRepSettings settings)
    {
        NotificationPreferencesDto? notifications = null;
        if (!string.IsNullOrEmpty(settings.NotificationPreferences))
        {
            notifications = JsonSerializer.Deserialize<NotificationPreferencesDto>(
                settings.NotificationPreferences, _jsonOptions);
        }

        AutoResponseTemplatesDto? autoResponse = null;
        if (!string.IsNullOrEmpty(settings.AutoResponseTemplates))
        {
            autoResponse = JsonSerializer.Deserialize<AutoResponseTemplatesDto>(
                settings.AutoResponseTemplates, _jsonOptions);
        }

        List<string>? disabledUsernames = null;
        if (!string.IsNullOrEmpty(settings.DisabledAccessUsernames))
        {
            disabledUsernames = JsonSerializer.Deserialize<List<string>>(
                settings.DisabledAccessUsernames, _jsonOptions);
        }

        return new BusinessRepSettingsDto(
            settings.Id,
            settings.BusinessRepId,
            notifications,
            settings.DarkMode,
            autoResponse,
            disabledUsernames,
            settings.CreatedAt,
            settings.UpdatedAt,
            settings.ModifiedByUserId
        );
    }
}