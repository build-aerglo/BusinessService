namespace BusinessService.Application.DTOs.Settings;

/// <summary>
/// Rep-level settings (controlled by each rep)
/// </summary>
public record BusinessRepSettingsDto(
    Guid Id,
    Guid BusinessRepId,
    NotificationPreferencesDto? NotificationPreferences,
    bool DarkMode,
    AutoResponseTemplatesDto? AutoResponseTemplates,
    List<string>? DisabledAccessUsernames,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid? ModifiedByUserId
);


/// <summary>
/// Request to update rep-level settings (any rep for their own)
/// </summary>
public class UpdateRepSettingsRequest
{

    public NotificationPreferencesDto? NotificationPreferences { get; set; }

    public bool? DarkMode { get; set; }

    public AutoResponseTemplatesDto? AutoResponseTemplates { get; set; } 
    public List<string>? DisabledAccessUsernames { get; set; }
    
}