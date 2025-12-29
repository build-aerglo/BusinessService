namespace BusinessService.Domain.Entities;

/// <summary>
/// Business settings controlled by parent business rep only.
/// Applies to the business and all its branches.
/// </summary>
public class BusinessSettings
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }

    // Review and Response Settings (parent rep only)
    public bool ReviewsPrivate { get; set; } = false;
    public DateTime? ReviewsPrivateEnabledAt { get; set; }
    public string? PrivateReviewsReason { get; set; }

    // Do Not Disturb Mode Settings (Enterprise only)
    public bool DndModeEnabled { get; set; } = false;
    public DateTime? DndModeEnabledAt { get; set; }
    public DateTime? DndModeExpiresAt { get; set; }
    public string? DndModeReason { get; set; }
    public int DndExtensionCount { get; set; } = 0;
    public string? DndModeMessage { get; set; }

    // Auto-response settings (Enterprise only)
    public bool AutoResponseEnabled { get; set; } = false;
    public DateTime? AutoResponseEnabledAt { get; set; }

    // External source integration settings
    public int ExternalSourcesConnected { get; set; } = 0;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedByUserId { get; set; }

    public const int MaxDndHours = 60;
    public const int MaxDndExtensions = 3;

    /// <summary>
    /// Enables DnD mode for specified hours (max 60 by default)
    /// </summary>
    public void EnableDndMode(int hours = 60, string? reason = null, string? message = null)
    {
        if (hours > MaxDndHours)
            hours = MaxDndHours;

        DndModeEnabled = true;
        DndModeEnabledAt = DateTime.UtcNow;
        DndModeExpiresAt = DateTime.UtcNow.AddHours(hours);
        DndModeReason = reason;
        DndModeMessage = message ?? "This business is temporarily not accepting new reviews.";
        DndExtensionCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Extends DnD mode (requires support contact for more than 3 extensions)
    /// </summary>
    public bool ExtendDndMode(int additionalHours)
    {
        if (!DndModeEnabled || !DndModeExpiresAt.HasValue)
            return false;

        if (DndExtensionCount >= MaxDndExtensions)
            return false;

        DndModeExpiresAt = DndModeExpiresAt.Value.AddHours(additionalHours);
        DndExtensionCount++;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Disables DnD mode
    /// </summary>
    public void DisableDndMode()
    {
        DndModeEnabled = false;
        DndModeEnabledAt = null;
        DndModeExpiresAt = null;
        DndModeReason = null;
        DndModeMessage = null;
        DndExtensionCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if DnD mode has expired and auto-disables if needed
    /// </summary>
    public bool CheckAndUpdateDndModeExpiry()
    {
        if (!DndModeEnabled || !DndModeExpiresAt.HasValue || DateTime.UtcNow < DndModeExpiresAt.Value)
            return false;

        DisableDndMode();
        return true;
    }

    /// <summary>
    /// Gets remaining DnD hours
    /// </summary>
    public double? GetRemainingDndHours()
    {
        if (!DndModeEnabled || !DndModeExpiresAt.HasValue)
            return null;

        var remaining = DndModeExpiresAt.Value - DateTime.UtcNow;
        return remaining.TotalHours > 0 ? remaining.TotalHours : 0;
    }

    /// <summary>
    /// Enables private reviews mode (Premium+ feature)
    /// </summary>
    public void EnablePrivateReviews(string? reason = null)
    {
        ReviewsPrivate = true;
        ReviewsPrivateEnabledAt = DateTime.UtcNow;
        PrivateReviewsReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables private reviews mode
    /// </summary>
    public void DisablePrivateReviews()
    {
        ReviewsPrivate = false;
        ReviewsPrivateEnabledAt = null;
        PrivateReviewsReason = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables auto-response feature (Enterprise only)
    /// </summary>
    public void EnableAutoResponse()
    {
        AutoResponseEnabled = true;
        AutoResponseEnabledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables auto-response feature
    /// </summary>
    public void DisableAutoResponse()
    {
        AutoResponseEnabled = false;
        AutoResponseEnabledAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}