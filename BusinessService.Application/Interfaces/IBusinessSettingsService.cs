using BusinessService.Application.DTOs.Settings;
using BusinessService.Application.DTOs;

namespace BusinessService.Application.Interfaces;

public interface IBusinessSettingsService
{
    // ========== Business Settings (Parent Rep Only) ==========
    
    /// <summary>
    /// Gets business settings. Creates default if none exist.
    /// </summary>
    Task<BusinessSettingsDto> GetBusinessSettingsAsync(Guid businessId);
    
    /// <summary>
    /// Updates business settings (DnD, ReviewsPrivate).
    /// Only parent rep can modify.
    /// </summary>
    Task<BusinessSettingsDto> UpdateBusinessSettingsAsync(
        Guid businessId, 
        UpdateBusinessSettingsRequest request,
        Guid currentUserId);
    
    /// <summary>
    /// Extends DnD mode duration (support users only)
    /// </summary>
    Task<BusinessSettingsDto> ExtendDndModeAsync(
        Guid businessId, 
        int additionalHours,
        Guid currentUserId);
    
    /// <summary>
    /// Background job: Auto-disable expired DnD modes
    /// </summary>
    Task ProcessExpiredDndModesAsync();
    
    // ========== Business Rep Settings (Any Rep) ==========
    
    /// <summary>
    /// Gets rep settings. Creates default if none exist.
    /// </summary>
    Task<BusinessRepSettingsDto> GetRepSettingsAsync(Guid businessRepId);
    
    /// <summary>
    /// Updates rep settings (Dark Mode, Notifications, etc.).
    /// Rep can only modify their own settings.
    /// </summary>
    Task<BusinessRepSettingsDto> UpdateRepSettingsAsync(
        Guid businessRepId,
        UpdateRepSettingsRequest request,
        Guid currentUserId);
    
    // ========== Combined View ==========
    
    /// <summary>
    /// Gets effective settings for a business rep (business + rep combined)
    /// </summary>
    Task<EffectiveSettingsDto> GetEffectiveSettingsAsync(Guid businessRepId);
}