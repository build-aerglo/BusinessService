using System.Text.Json;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;
using Microsoft.Extensions.Logging;

namespace BusinessService.Infrastructure.Repositories;

/// <summary>
/// Reads pre-calculated analytics data from the business_analytics table.
/// The Azure Function (AnalyticsProcessorFunction) is the sole writer.
/// This repository only ever SELECTs — never INSERTs or UPDATEs.
///
/// JSONB handling:
///   The metrics column is cast to ::text so Dapper can read it as a plain string.
///   We then deserialize it ourselves using System.Text.Json.
/// </summary>
public class AnalyticsReadRepository : IAnalyticsReadRepository
{
    private readonly DapperContext _context;
    private readonly ILogger<AnalyticsReadRepository> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AnalyticsReadRepository(DapperContext context, ILogger<AnalyticsReadRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BusinessAnalyticsDashboard?> GetDashboardAsync(Guid businessId)
    {
        const string sql = """
            SELECT
                id,
                business_id,
                total_reviews,
                average_rating,
                bayesian_average_rating,
                metrics::text      AS metrics_json,
                last_calculated_at,
                created_at,
                updated_at
            FROM business_analytics
            WHERE business_id = @BusinessId;
            """;

        try
        {
            using var conn = _context.CreateConnection();

            var row = await conn.QuerySingleOrDefaultAsync<AnalyticsRawRow>(sql, new { BusinessId = businessId });

            if (row == null)
            {
                _logger.LogDebug("No analytics row found for business {BusinessId}.", businessId);
                return null;
            }

            return Map(row);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch analytics for business {BusinessId}.", businessId);
            throw;
        }
    }

    // ── Mapping ──────────────────────────────────────────

    private BusinessAnalyticsDashboard Map(AnalyticsRawRow row)
    {
        AnalyticsMetrics? metrics = null;

        if (!string.IsNullOrWhiteSpace(row.MetricsJson) && row.MetricsJson != "{}")
        {
            try
            {
                metrics = JsonSerializer.Deserialize<AnalyticsMetrics>(row.MetricsJson, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize metrics JSON: {Json}", row.MetricsJson);
                metrics = null;
            }
        }

        return new BusinessAnalyticsDashboard
        {
            Id                    = row.Id,
            BusinessId            = row.BusinessId,
            TotalReviews          = row.TotalReviews,
            AverageRating         = row.AverageRating,
            BayesianAverageRating = row.BayesianAverageRating,
            Metrics               = metrics,
            LastCalculatedAt      = row.LastCalculatedAt,
            CreatedAt             = row.CreatedAt,
            UpdatedAt             = row.UpdatedAt
        };
    }

    // ── Private raw row ───────────────────────────────────
    // Dapper maps snake_case column aliases to PascalCase properties
    // because DefaultTypeMap.MatchNamesWithUnderscores = true is set in Program.cs

    private sealed class AnalyticsRawRow
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public int TotalReviews { get; set; }
        public decimal AverageRating { get; set; }
        public decimal BayesianAverageRating { get; set; }
        public string MetricsJson { get; set; } = "{}";
        public DateTime LastCalculatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}