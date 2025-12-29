namespace BusinessService.Application.DTOs.Settings;

/// <summary>
/// Business-level settings (controlled by parent rep only)
/// </summary>
public record BusinessSettingsDto(
    Guid Id,
    Guid BusinessId,

    // Private Reviews (Premium+)
    bool ReviewsPrivate,
    DateTime? ReviewsPrivateEnabledAt,
    string? PrivateReviewsReason,

    // DnD Mode (Enterprise)
    bool DndModeEnabled,
    DateTime? DndModeEnabledAt,
    DateTime? DndModeExpiresAt,
    string? DndModeReason,
    int DndExtensionCount,
    string? DndModeMessage,
    double? RemainingDndHours,

    // Auto-response (Enterprise)
    bool AutoResponseEnabled,
    DateTime? AutoResponseEnabledAt,

    // External sources
    int ExternalSourcesConnected,

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
    public string? PrivateReviewsReason { get; set; }
    public bool? DndModeEnabled { get; set; }
    public int? DndModeDurationHours { get; set; }
    public string? DndModeReason { get; set; }
    public string? DndModeMessage { get; set; }
    public bool? AutoResponseEnabled { get; set; }
}

/// <summary>
/// Request to enable DnD mode
/// </summary>
public class EnableDndModeRequest
{
    public Guid BusinessId { get; set; }
    public int Hours { get; set; } = 60;
    public string? Reason { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Request to extend DnD mode
/// </summary>
public class ExtendDndModeRequest
{
    public Guid BusinessId { get; set; }
    public int AdditionalHours { get; set; }
}

/// <summary>
/// DnD mode status response
/// </summary>
public record DndModeStatusResponse(
    bool IsEnabled,
    DateTime? EnabledAt,
    DateTime? ExpiresAt,
    double? RemainingHours,
    int ExtensionCount,
    int MaxExtensions,
    bool CanExtend,
    string? Message
);

/// <summary>
/// Request to enable private reviews mode
/// </summary>
public class EnablePrivateReviewsRequest
{
    public Guid BusinessId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Private reviews status response
/// </summary>
public record PrivateReviewsStatusResponse(
    bool IsEnabled,
    DateTime? EnabledAt,
    string? Reason,
    string ConsumerMessage
);