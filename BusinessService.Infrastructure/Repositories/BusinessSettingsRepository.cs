using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class BusinessSettingsRepository : IBusinessSettingsRepository
{
    private readonly DapperContext _context;

    public BusinessSettingsRepository(DapperContext context)
    {
        _context = context;
    }

    // ========== Business Settings ==========

    public async Task<BusinessSettings?> FindBusinessSettingsByBusinessIdAsync(Guid businessId)
    {
        const string sql = @"
            SELECT 
                id AS Id, 
                business_id AS BusinessId, 
                reviews_private AS ReviewsPrivate,
                dnd_mode_enabled AS DndModeEnabled, 
                dnd_mode_enabled_at AS DndModeEnabledAt,
                dnd_mode_expires_at AS DndModeExpiresAt,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt, 
                modified_by_user_id AS ModifiedByUserId
            FROM business_settings
            WHERE business_id = @businessId;
        ";

        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessSettings>(sql, new { businessId });
    }

    public async Task<BusinessSettings> AddBusinessSettingsAsync(BusinessSettings settings)
    {
        const string sql = @"
            INSERT INTO business_settings (
                id, business_id, reviews_private, 
                dnd_mode_enabled, dnd_mode_enabled_at, dnd_mode_expires_at,
                created_at, updated_at, modified_by_user_id
            ) VALUES (
                @Id, @BusinessId, @ReviewsPrivate,
                @DndModeEnabled, @DndModeEnabledAt, @DndModeExpiresAt,
                @CreatedAt, @UpdatedAt, @ModifiedByUserId
            );
        ";

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, settings);
        return settings;
    }

    public async Task UpdateBusinessSettingsAsync(BusinessSettings settings)
    {
        const string sql = @"
            UPDATE business_settings
            SET 
                reviews_private = @ReviewsPrivate,
                dnd_mode_enabled = @DndModeEnabled,
                dnd_mode_enabled_at = @DndModeEnabledAt,
                dnd_mode_expires_at = @DndModeExpiresAt,
                updated_at = @UpdatedAt,
                modified_by_user_id = @ModifiedByUserId
            WHERE business_id = @BusinessId;
        ";

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, settings);
    }

    public async Task<bool> BusinessSettingsExistAsync(Guid businessId)
    {
        const string sql = @"
            SELECT EXISTS (
                SELECT 1 FROM business_settings 
                WHERE business_id = @businessId
            );
        ";

        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { businessId });
    }

    public async Task<List<BusinessSettings>> GetExpiredDndModeSettingsAsync()
    {
        const string sql = @"
            SELECT 
                id AS Id, 
                business_id AS BusinessId, 
                reviews_private AS ReviewsPrivate,
                dnd_mode_enabled AS DndModeEnabled, 
                dnd_mode_enabled_at AS DndModeEnabledAt,
                dnd_mode_expires_at AS DndModeExpiresAt,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt, 
                modified_by_user_id AS ModifiedByUserId
            FROM business_settings
            WHERE dnd_mode_enabled = true
              AND dnd_mode_expires_at IS NOT NULL
              AND dnd_mode_expires_at <= @now;
        ";

        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessSettings>(sql, new { now = DateTime.UtcNow });
        return results.ToList();
    }

    // ========== Business Rep Settings ==========

    public async Task<BusinessRepSettings?> FindRepSettingsByRepIdAsync(Guid businessRepId)
    {
        const string sql = @"
            SELECT 
                id AS Id,
                business_rep_id AS BusinessRepId,
                notification_preferences AS NotificationPreferences,
                dark_mode AS DarkMode,
                auto_response_templates AS AutoResponseTemplates,
                disabled_access_usernames AS DisabledAccessUsernames,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt,
                modified_by_user_id AS ModifiedByUserId
            FROM business_rep_settings
            WHERE business_rep_id = @businessRepId;
        ";

        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessRepSettings>(sql, new { businessRepId });
    }

    public async Task<BusinessRepSettings> AddRepSettingsAsync(BusinessRepSettings settings)
    {
        const string sql = @"
            INSERT INTO business_rep_settings (
                id, business_rep_id, notification_preferences, dark_mode,
                auto_response_templates, disabled_access_usernames,
                created_at, updated_at, modified_by_user_id
            ) VALUES (
                @Id, @BusinessRepId, @NotificationPreferences::jsonb, @DarkMode,
                @AutoResponseTemplates::jsonb, @DisabledAccessUsernames::jsonb,
                @CreatedAt, @UpdatedAt, @ModifiedByUserId
            );
        ";

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, settings);
        return settings;
    }

    public async Task UpdateRepSettingsAsync(BusinessRepSettings settings)
    {
        const string sql = @"
            UPDATE business_rep_settings
            SET 
                notification_preferences = @NotificationPreferences::jsonb,
                dark_mode = @DarkMode,
                auto_response_templates = @AutoResponseTemplates::jsonb,
                disabled_access_usernames = @DisabledAccessUsernames::jsonb,
                updated_at = @UpdatedAt,
                modified_by_user_id = @ModifiedByUserId
            WHERE business_rep_id = @BusinessRepId;
        ";

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, settings);
    }

    public async Task<bool> RepSettingsExistAsync(Guid businessRepId)
    {
        const string sql = @"
            SELECT EXISTS (
                SELECT 1 FROM business_rep_settings 
                WHERE business_rep_id = @businessRepId
            );
        ";

        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { businessRepId });
    }
}