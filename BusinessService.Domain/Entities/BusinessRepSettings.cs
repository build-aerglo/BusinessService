namespace BusinessService.Domain.Entities;

/// <summary>
/// Business rep settings controlled by each individual rep.
/// Only affects the specific rep's preferences.
/// </summary>
public class BusinessRepSettings
{
    public Guid Id { get; set; }
    public Guid BusinessRepId { get; set; }
    
    // Preference Settings (each rep controls their own)
    public string? NotificationPreferences { get; set; } // JSON: {email, whatsapp, inApp}
    public bool DarkMode { get; set; } = false;
    
    // Review Response Settings (each rep controls their own)
    public string? AutoResponseTemplates { get; set; } // JSON: {positive, negative, neutral}
    
    // User Management Settings (each rep controls their own)
    public string? DisabledAccessUsernames { get; set; } // JSON array of disabled usernames
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedByUserId { get; set; }
}