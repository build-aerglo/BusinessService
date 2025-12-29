using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class ExternalSourceRepository : IExternalSourceRepository
{
    private readonly DapperContext _context;

    public ExternalSourceRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<ExternalSource?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM external_source WHERE id = @id;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<ExternalSource>(sql, new { id });
    }

    public async Task<List<ExternalSource>> FindByBusinessIdAsync(Guid businessId)
    {
        const string sql = "SELECT * FROM external_source WHERE business_id = @businessId ORDER BY created_at;";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<ExternalSource>(sql, new { businessId });
        return results.ToList();
    }

    public async Task<List<ExternalSource>> FindConnectedByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT * FROM external_source
            WHERE business_id = @businessId
            AND status = @status
            ORDER BY source_type;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<ExternalSource>(sql, new
        {
            businessId,
            status = (int)ExternalSourceStatus.Connected
        });
        return results.ToList();
    }

    public async Task<ExternalSource?> FindByBusinessIdAndTypeAsync(Guid businessId, ExternalSourceType sourceType)
    {
        const string sql = """
            SELECT * FROM external_source
            WHERE business_id = @businessId
            AND source_type = @sourceType
            LIMIT 1;
        """;
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<ExternalSource>(sql, new
        {
            businessId,
            sourceType = (int)sourceType
        });
    }

    public async Task AddAsync(ExternalSource source)
    {
        const string sql = """
            INSERT INTO external_source (
                id, business_id, source_type, source_name, source_url, source_account_id,
                status, connected_at, last_sync_at, next_sync_at, last_sync_error,
                auto_sync_enabled, sync_interval_hours, total_reviews_imported, reviews_imported_last_sync,
                access_token, refresh_token, token_expires_at,
                created_at, updated_at, connected_by_user_id
            ) VALUES (
                @Id, @BusinessId, @SourceType, @SourceName, @SourceUrl, @SourceAccountId,
                @Status, @ConnectedAt, @LastSyncAt, @NextSyncAt, @LastSyncError,
                @AutoSyncEnabled, @SyncIntervalHours, @TotalReviewsImported, @ReviewsImportedLastSync,
                @AccessToken, @RefreshToken, @TokenExpiresAt,
                @CreatedAt, @UpdatedAt, @ConnectedByUserId
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, source);
    }

    public async Task UpdateAsync(ExternalSource source)
    {
        const string sql = """
            UPDATE external_source SET
                source_name = @SourceName, source_url = @SourceUrl, source_account_id = @SourceAccountId,
                status = @Status, connected_at = @ConnectedAt, last_sync_at = @LastSyncAt,
                next_sync_at = @NextSyncAt, last_sync_error = @LastSyncError,
                auto_sync_enabled = @AutoSyncEnabled, sync_interval_hours = @SyncIntervalHours,
                total_reviews_imported = @TotalReviewsImported, reviews_imported_last_sync = @ReviewsImportedLastSync,
                access_token = @AccessToken, refresh_token = @RefreshToken, token_expires_at = @TokenExpiresAt,
                updated_at = @UpdatedAt
            WHERE id = @Id;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, source);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM external_source WHERE id = @id;";
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new { id });
    }

    public async Task<int> CountConnectedByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT COUNT(*) FROM external_source
            WHERE business_id = @businessId
            AND status = @status;
        """;
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            businessId,
            status = (int)ExternalSourceStatus.Connected
        });
    }

    public async Task<List<ExternalSource>> FindDueSyncAsync()
    {
        const string sql = """
            SELECT * FROM external_source
            WHERE status = @status
            AND auto_sync_enabled = true
            AND next_sync_at <= @now;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<ExternalSource>(sql, new
        {
            status = (int)ExternalSourceStatus.Connected,
            now = DateTime.UtcNow
        });
        return results.ToList();
    }

    public async Task<bool> ExistsByBusinessIdAndTypeAsync(Guid businessId, ExternalSourceType sourceType)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM external_source
                WHERE business_id = @businessId
                AND source_type = @sourceType
            );
        """;
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new
        {
            businessId,
            sourceType = (int)sourceType
        });
    }
}
