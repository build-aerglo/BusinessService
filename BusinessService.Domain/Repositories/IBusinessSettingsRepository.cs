using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

/// <summary>
/// Repository for managing both business settings and rep settings
/// </summary>
public interface IBusinessSettingsRepository
{
    // ========== Business Settings ==========
    
    /// <summary>
    /// Gets business settings for a specific business
    /// </summary>
    Task<BusinessSettings?> FindBusinessSettingsByBusinessIdAsync(Guid businessId);
    
    /// <summary>
    /// Creates new business settings
    /// </summary>
    Task<BusinessSettings> AddBusinessSettingsAsync(BusinessSettings settings);
    
    /// <summary>
    /// Updates existing business settings
    /// </summary>
    Task UpdateBusinessSettingsAsync(BusinessSettings settings);
    
    /// <summary>
    /// Checks if business settings exist
    /// </summary>
    Task<bool> BusinessSettingsExistAsync(Guid businessId);
    
    /// <summary>
    /// Gets all businesses with expired DnD mode
    /// </summary>
    Task<List<BusinessSettings>> GetExpiredDndModeSettingsAsync();
    
    // ========== Business Rep Settings ==========
    
    /// <summary>
    /// Gets rep settings for a specific business rep
    /// </summary>
    Task<BusinessRepSettings?> FindRepSettingsByRepIdAsync(Guid businessRepId);
    
    /// <summary>
    /// Creates new rep settings
    /// </summary>
    Task<BusinessRepSettings> AddRepSettingsAsync(BusinessRepSettings settings);
    
    /// <summary>
    /// Updates existing rep settings
    /// </summary>
    Task UpdateRepSettingsAsync(BusinessRepSettings settings);
    
    /// <summary>
    /// Checks if rep settings exist
    /// </summary>
    Task<bool> RepSettingsExistAsync(Guid businessRepId);
}