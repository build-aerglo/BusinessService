using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class BusinessAnalyticsRepository : IBusinessAnalyticsRepository
{
    private readonly DapperContext _context;

    public BusinessAnalyticsRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<BusinessAnalytics?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM business_analytics WHERE id = @id;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessAnalytics>(sql, new { id });
    }

    public async Task<BusinessAnalytics?> FindByBusinessIdAndPeriodAsync(Guid businessId, DateTime periodStart, DateTime periodEnd)
    {
        const string sql = """
            SELECT * FROM business_analytics
            WHERE business_id = @businessId
            AND period_start = @periodStart
            AND period_end = @periodEnd
            LIMIT 1;
        """;
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessAnalytics>(sql, new { businessId, periodStart, periodEnd });
    }

    public async Task<BusinessAnalytics?> FindLatestByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT * FROM business_analytics
            WHERE business_id = @businessId
            ORDER BY period_end DESC
            LIMIT 1;
        """;
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessAnalytics>(sql, new { businessId });
    }

    public async Task<List<BusinessAnalytics>> FindByBusinessIdAsync(Guid businessId, int limit = 12)
    {
        const string sql = """
            SELECT * FROM business_analytics
            WHERE business_id = @businessId
            ORDER BY period_end DESC
            LIMIT @limit;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessAnalytics>(sql, new { businessId, limit });
        return results.ToList();
    }

    public async Task AddAsync(BusinessAnalytics analytics)
    {
        const string sql = """
            INSERT INTO business_analytics (
                id, business_id, period_start, period_end, period_type,
                average_rating, rating_change, total_reviews, new_reviews,
                positive_reviews, neutral_reviews, negative_reviews, sentiment_score,
                total_responses, response_rate, average_response_time_hours,
                helpful_votes, profile_views, qr_code_scans,
                top_complaints_json, top_praise_json, keyword_cloud_json,
                created_at, updated_at
            ) VALUES (
                @Id, @BusinessId, @PeriodStart, @PeriodEnd, @PeriodType,
                @AverageRating, @RatingChange, @TotalReviews, @NewReviews,
                @PositiveReviews, @NeutralReviews, @NegativeReviews, @SentimentScore,
                @TotalResponses, @ResponseRate, @AverageResponseTimeHours,
                @HelpfulVotes, @ProfileViews, @QrCodeScans,
                @TopComplaintsJson, @TopPraiseJson, @KeywordCloudJson,
                @CreatedAt, @UpdatedAt
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, analytics);
    }

    public async Task UpdateAsync(BusinessAnalytics analytics)
    {
        const string sql = """
            UPDATE business_analytics SET
                average_rating = @AverageRating, rating_change = @RatingChange,
                total_reviews = @TotalReviews, new_reviews = @NewReviews,
                positive_reviews = @PositiveReviews, neutral_reviews = @NeutralReviews,
                negative_reviews = @NegativeReviews, sentiment_score = @SentimentScore,
                total_responses = @TotalResponses, response_rate = @ResponseRate,
                average_response_time_hours = @AverageResponseTimeHours,
                helpful_votes = @HelpfulVotes, profile_views = @ProfileViews, qr_code_scans = @QrCodeScans,
                top_complaints_json = @TopComplaintsJson, top_praise_json = @TopPraiseJson,
                keyword_cloud_json = @KeywordCloudJson, updated_at = @UpdatedAt
            WHERE id = @Id;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, analytics);
    }
}

public class BranchComparisonRepository : IBranchComparisonRepository
{
    private readonly DapperContext _context;

    public BranchComparisonRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<BranchComparisonSnapshot?> FindLatestByParentBusinessIdAsync(Guid parentBusinessId)
    {
        const string sql = """
            SELECT * FROM branch_comparison_snapshot
            WHERE parent_business_id = @parentBusinessId
            ORDER BY snapshot_date DESC
            LIMIT 1;
        """;
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BranchComparisonSnapshot>(sql, new { parentBusinessId });
    }

    public async Task<List<BranchComparisonSnapshot>> FindByParentBusinessIdAsync(Guid parentBusinessId, int limit = 12)
    {
        const string sql = """
            SELECT * FROM branch_comparison_snapshot
            WHERE parent_business_id = @parentBusinessId
            ORDER BY snapshot_date DESC
            LIMIT @limit;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BranchComparisonSnapshot>(sql, new { parentBusinessId, limit });
        return results.ToList();
    }

    public async Task AddAsync(BranchComparisonSnapshot snapshot)
    {
        const string sql = """
            INSERT INTO branch_comparison_snapshot (
                id, parent_business_id, snapshot_date, branch_metrics_json,
                top_performing_branch_id, lowest_performing_branch_id,
                average_rating_across_branches, total_reviews_across_branches, created_at
            ) VALUES (
                @Id, @ParentBusinessId, @SnapshotDate, @BranchMetricsJson,
                @TopPerformingBranchId, @LowestPerformingBranchId,
                @AverageRatingAcrossBranches, @TotalReviewsAcrossBranches, @CreatedAt
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, snapshot);
    }
}

public class CompetitorComparisonRepository : ICompetitorComparisonRepository
{
    private readonly DapperContext _context;

    public CompetitorComparisonRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<CompetitorComparison?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM competitor_comparison WHERE id = @id;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<CompetitorComparison>(sql, new { id });
    }

    public async Task<List<CompetitorComparison>> FindByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT * FROM competitor_comparison
            WHERE business_id = @businessId
            ORDER BY display_order;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<CompetitorComparison>(sql, new { businessId });
        return results.ToList();
    }

    public async Task<List<CompetitorComparison>> FindActiveByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT * FROM competitor_comparison
            WHERE business_id = @businessId
            AND is_active = true
            ORDER BY display_order;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<CompetitorComparison>(sql, new { businessId });
        return results.ToList();
    }

    public async Task AddAsync(CompetitorComparison comparison)
    {
        const string sql = """
            INSERT INTO competitor_comparison (
                id, business_id, competitor_business_id, competitor_name, competitor_category_id,
                is_active, display_order, created_at, updated_at, added_by_user_id
            ) VALUES (
                @Id, @BusinessId, @CompetitorBusinessId, @CompetitorName, @CompetitorCategoryId,
                @IsActive, @DisplayOrder, @CreatedAt, @UpdatedAt, @AddedByUserId
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, comparison);
    }

    public async Task UpdateAsync(CompetitorComparison comparison)
    {
        const string sql = """
            UPDATE competitor_comparison SET
                competitor_name = @CompetitorName, competitor_category_id = @CompetitorCategoryId,
                is_active = @IsActive, display_order = @DisplayOrder, updated_at = @UpdatedAt
            WHERE id = @Id;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, comparison);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM competitor_comparison WHERE id = @id;";
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new { id });
    }

    public async Task<int> CountActiveByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT COUNT(*) FROM competitor_comparison
            WHERE business_id = @businessId
            AND is_active = true;
        """;
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { businessId });
    }

    public async Task<bool> ExistsByBusinessAndCompetitorAsync(Guid businessId, Guid competitorBusinessId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM competitor_comparison
                WHERE business_id = @businessId
                AND competitor_business_id = @competitorBusinessId
            );
        """;
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { businessId, competitorBusinessId });
    }
}

public class CompetitorComparisonSnapshotRepository : ICompetitorComparisonSnapshotRepository
{
    private readonly DapperContext _context;

    public CompetitorComparisonSnapshotRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<CompetitorComparisonSnapshot?> FindLatestByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT * FROM competitor_comparison_snapshot
            WHERE business_id = @businessId
            ORDER BY snapshot_date DESC
            LIMIT 1;
        """;
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<CompetitorComparisonSnapshot>(sql, new { businessId });
    }

    public async Task<List<CompetitorComparisonSnapshot>> FindByBusinessIdAsync(Guid businessId, int limit = 12)
    {
        const string sql = """
            SELECT * FROM competitor_comparison_snapshot
            WHERE business_id = @businessId
            ORDER BY snapshot_date DESC
            LIMIT @limit;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<CompetitorComparisonSnapshot>(sql, new { businessId, limit });
        return results.ToList();
    }

    public async Task AddAsync(CompetitorComparisonSnapshot snapshot)
    {
        const string sql = """
            INSERT INTO competitor_comparison_snapshot (
                id, business_id, snapshot_date, period_type, comparison_data_json,
                competitors_compared, ranking_position, average_rating_difference, created_at
            ) VALUES (
                @Id, @BusinessId, @SnapshotDate, @PeriodType, @ComparisonDataJson,
                @CompetitorsCompared, @RankingPosition, @AverageRatingDifference, @CreatedAt
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, snapshot);
    }
}
