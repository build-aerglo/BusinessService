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
    
    // Password and Security Settings (parent rep only)
    public bool DndModeEnabled { get; set; } = false;
    public DateTime? DndModeEnabledAt { get; set; }
    public DateTime? DndModeExpiresAt { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedByUserId { get; set; }
    
    /// <summary>
    /// Enables DnD mode for specified hours (default: 60)
    /// </summary>
    public void EnableDndMode(int hours = 60)
    {
        DndModeEnabled = true;
        DndModeEnabledAt = DateTime.UtcNow;
        DndModeExpiresAt = DateTime.UtcNow.AddHours(hours);
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Disables DnD mode
    /// </summary>
    public void DisableDndMode()
    {
        DndModeEnabled = false;
        DndModeEnabledAt = null;
        DndModeExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Checks if DnD mode has expired and auto-disables if needed
    /// </summary>
    public bool CheckAndUpdateDndModeExpiry()
    {
        if (DndModeEnabled && DndModeExpiresAt.HasValue && DateTime.UtcNow >= DndModeExpiresAt.Value)
        {
            DisableDndMode();
            return true;
        }
        return false;
    }
}