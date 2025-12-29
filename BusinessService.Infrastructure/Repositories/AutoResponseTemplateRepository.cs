using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class AutoResponseTemplateRepository : IAutoResponseTemplateRepository
{
    private readonly DapperContext _context;

    public AutoResponseTemplateRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<AutoResponseTemplate?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM auto_response_template WHERE id = @id;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<AutoResponseTemplate>(sql, new { id });
    }

    public async Task<List<AutoResponseTemplate>> FindByBusinessIdAsync(Guid businessId)
    {
        const string sql = "SELECT * FROM auto_response_template WHERE business_id = @businessId ORDER BY sentiment, priority;";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<AutoResponseTemplate>(sql, new { businessId });
        return results.ToList();
    }

    public async Task<List<AutoResponseTemplate>> FindActiveByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT * FROM auto_response_template
            WHERE business_id = @businessId AND is_active = true
            ORDER BY sentiment, priority;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<AutoResponseTemplate>(sql, new { businessId });
        return results.ToList();
    }

    public async Task<AutoResponseTemplate?> FindDefaultBySentimentAsync(Guid businessId, ReviewSentiment sentiment)
    {
        const string sql = """
            SELECT * FROM auto_response_template
            WHERE business_id = @businessId
            AND sentiment = @sentiment
            AND is_default = true
            AND is_active = true
            LIMIT 1;
        """;
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<AutoResponseTemplate>(sql, new
        {
            businessId,
            sentiment = (int)sentiment
        });
    }

    public async Task<AutoResponseTemplate?> FindMatchingTemplateAsync(Guid businessId, ReviewSentiment sentiment, int starRating)
    {
        const string sql = """
            SELECT * FROM auto_response_template
            WHERE business_id = @businessId
            AND sentiment = @sentiment
            AND is_active = true
            AND (min_star_rating IS NULL OR min_star_rating <= @starRating)
            AND (max_star_rating IS NULL OR max_star_rating >= @starRating)
            ORDER BY priority, is_default DESC
            LIMIT 1;
        """;
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<AutoResponseTemplate>(sql, new
        {
            businessId,
            sentiment = (int)sentiment,
            starRating
        });
    }

    public async Task AddAsync(AutoResponseTemplate template)
    {
        const string sql = """
            INSERT INTO auto_response_template (
                id, business_id, name, sentiment, template_content,
                is_active, is_default, priority, min_star_rating, max_star_rating,
                times_used, last_used_at, created_at, updated_at, created_by_user_id
            ) VALUES (
                @Id, @BusinessId, @Name, @Sentiment, @TemplateContent,
                @IsActive, @IsDefault, @Priority, @MinStarRating, @MaxStarRating,
                @TimesUsed, @LastUsedAt, @CreatedAt, @UpdatedAt, @CreatedByUserId
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, template);
    }

    public async Task UpdateAsync(AutoResponseTemplate template)
    {
        const string sql = """
            UPDATE auto_response_template SET
                name = @Name, sentiment = @Sentiment, template_content = @TemplateContent,
                is_active = @IsActive, is_default = @IsDefault, priority = @Priority,
                min_star_rating = @MinStarRating, max_star_rating = @MaxStarRating,
                times_used = @TimesUsed, last_used_at = @LastUsedAt, updated_at = @UpdatedAt
            WHERE id = @Id;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, template);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM auto_response_template WHERE id = @id;";
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new { id });
    }

    public async Task<bool> ExistsByNameAndBusinessIdAsync(string name, Guid businessId)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM auto_response_template WHERE LOWER(name) = LOWER(@name) AND business_id = @businessId);";
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { name, businessId });
    }

    public async Task IncrementUsageAsync(Guid templateId)
    {
        const string sql = """
            UPDATE auto_response_template SET
                times_used = times_used + 1,
                last_used_at = @lastUsedAt,
                updated_at = @updatedAt
            WHERE id = @templateId;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            templateId,
            lastUsedAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        });
    }
}
