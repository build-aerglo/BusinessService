using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly DapperContext _context;

    public SubscriptionPlanRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlan?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM subscription_plan WHERE id = @id;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<SubscriptionPlan>(sql, new { id });
    }

    public async Task<SubscriptionPlan?> FindByTierAsync(SubscriptionTier tier)
    {
        const string sql = "SELECT * FROM subscription_plan WHERE tier = @tier AND is_active = true;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<SubscriptionPlan>(sql, new { tier = (int)tier });
    }

    public async Task<List<SubscriptionPlan>> GetAllActiveAsync()
    {
        const string sql = "SELECT * FROM subscription_plan WHERE is_active = true ORDER BY tier;";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<SubscriptionPlan>(sql);
        return results.ToList();
    }

    public async Task AddAsync(SubscriptionPlan plan)
    {
        const string sql = """
            INSERT INTO subscription_plan (
                id, name, tier, description, monthly_price, annual_price, currency,
                monthly_reply_limit, monthly_dispute_limit, external_source_limit, user_login_limit,
                private_reviews_enabled, data_api_enabled, dnd_mode_enabled, auto_response_enabled,
                branch_comparison_enabled, competitor_comparison_enabled, is_active, created_at, updated_at
            ) VALUES (
                @Id, @Name, @Tier, @Description, @MonthlyPrice, @AnnualPrice, @Currency,
                @MonthlyReplyLimit, @MonthlyDisputeLimit, @ExternalSourceLimit, @UserLoginLimit,
                @PrivateReviewsEnabled, @DataApiEnabled, @DndModeEnabled, @AutoResponseEnabled,
                @BranchComparisonEnabled, @CompetitorComparisonEnabled, @IsActive, @CreatedAt, @UpdatedAt
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, plan);
    }

    public async Task UpdateAsync(SubscriptionPlan plan)
    {
        const string sql = """
            UPDATE subscription_plan SET
                name = @Name, tier = @Tier, description = @Description,
                monthly_price = @MonthlyPrice, annual_price = @AnnualPrice, currency = @Currency,
                monthly_reply_limit = @MonthlyReplyLimit, monthly_dispute_limit = @MonthlyDisputeLimit,
                external_source_limit = @ExternalSourceLimit, user_login_limit = @UserLoginLimit,
                private_reviews_enabled = @PrivateReviewsEnabled, data_api_enabled = @DataApiEnabled,
                dnd_mode_enabled = @DndModeEnabled, auto_response_enabled = @AutoResponseEnabled,
                branch_comparison_enabled = @BranchComparisonEnabled, competitor_comparison_enabled = @CompetitorComparisonEnabled,
                is_active = @IsActive, updated_at = @UpdatedAt
            WHERE id = @Id;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, plan);
    }
}

public class BusinessSubscriptionRepository : IBusinessSubscriptionRepository
{
    private readonly DapperContext _context;

    public BusinessSubscriptionRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<BusinessSubscription?> FindByBusinessIdAsync(Guid businessId)
    {
        const string sql = "SELECT * FROM business_subscription WHERE business_id = @businessId ORDER BY created_at DESC LIMIT 1;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessSubscription>(sql, new { businessId });
    }

    public async Task<BusinessSubscription?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM business_subscription WHERE id = @id;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessSubscription>(sql, new { id });
    }

    public async Task<BusinessSubscription?> FindActiveByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT * FROM business_subscription
            WHERE business_id = @businessId
            AND status = @status
            AND end_date > @now
            ORDER BY created_at DESC LIMIT 1;
        """;
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessSubscription>(sql, new
        {
            businessId,
            status = (int)SubscriptionStatus.Active,
            now = DateTime.UtcNow
        });
    }

    public async Task AddAsync(BusinessSubscription subscription)
    {
        const string sql = """
            INSERT INTO business_subscription (
                id, business_id, subscription_plan_id, start_date, end_date, billing_date, is_annual,
                status, cancelled_at, cancellation_reason,
                replies_used_this_month, disputes_used_this_month, usage_reset_date,
                created_at, updated_at
            ) VALUES (
                @Id, @BusinessId, @SubscriptionPlanId, @StartDate, @EndDate, @BillingDate, @IsAnnual,
                @Status, @CancelledAt, @CancellationReason,
                @RepliesUsedThisMonth, @DisputesUsedThisMonth, @UsageResetDate,
                @CreatedAt, @UpdatedAt
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, subscription);
    }

    public async Task UpdateAsync(BusinessSubscription subscription)
    {
        const string sql = """
            UPDATE business_subscription SET
                subscription_plan_id = @SubscriptionPlanId, start_date = @StartDate, end_date = @EndDate,
                billing_date = @BillingDate, is_annual = @IsAnnual, status = @Status,
                cancelled_at = @CancelledAt, cancellation_reason = @CancellationReason,
                replies_used_this_month = @RepliesUsedThisMonth, disputes_used_this_month = @DisputesUsedThisMonth,
                usage_reset_date = @UsageResetDate, updated_at = @UpdatedAt
            WHERE id = @Id;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, subscription);
    }

    public async Task<List<BusinessSubscription>> FindExpiringAsync(int daysUntilExpiry)
    {
        const string sql = """
            SELECT * FROM business_subscription
            WHERE status = @status
            AND end_date BETWEEN @now AND @expiryDate;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessSubscription>(sql, new
        {
            status = (int)SubscriptionStatus.Active,
            now = DateTime.UtcNow,
            expiryDate = DateTime.UtcNow.AddDays(daysUntilExpiry)
        });
        return results.ToList();
    }

    public async Task<List<BusinessSubscription>> FindByStatusAsync(SubscriptionStatus status)
    {
        const string sql = "SELECT * FROM business_subscription WHERE status = @status;";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessSubscription>(sql, new { status = (int)status });
        return results.ToList();
    }

    public async Task UpdateUsageAsync(Guid subscriptionId, int repliesUsed, int disputesUsed)
    {
        const string sql = """
            UPDATE business_subscription SET
                replies_used_this_month = @repliesUsed,
                disputes_used_this_month = @disputesUsed,
                updated_at = @updatedAt
            WHERE id = @subscriptionId;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new { subscriptionId, repliesUsed, disputesUsed, updatedAt = DateTime.UtcNow });
    }
}
