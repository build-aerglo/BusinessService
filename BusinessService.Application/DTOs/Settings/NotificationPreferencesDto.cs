namespace BusinessService.Application.DTOs.Settings;

/// <summary>
/// Notification preferences structure
/// </summary>
public record NotificationPreferencesDto(
    bool Email,
    bool Whatsapp,
    bool InApp
);
