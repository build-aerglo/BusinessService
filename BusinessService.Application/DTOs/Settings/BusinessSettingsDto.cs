namespace BusinessService.Application.DTOs.Settings;

/// <summary>
/// Business-level settings (controlled by parent rep only)
/// </summary>
public record BusinessSettingsDto(
    Guid Id,
    Guid BusinessId,
    bool ReviewsPrivate,
    bool DndModeEnabled,
    DateTime? DndModeEnabledAt,
    DateTime? DndModeExpiresAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid? ModifiedByUserId
);

/// <summary>
/// Request to update business-level settings (parent rep only)
/// </summary>
public class UpdateBusinessSettingsRequest
{
    public bool? ReviewsPrivate { get; set; }
    public bool? DndModeEnabled { get; set; }
    public int? DndModeDurationHours { get; set; }
}