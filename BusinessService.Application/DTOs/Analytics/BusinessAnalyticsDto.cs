using BusinessService.Domain.Entities;

namespace BusinessService.Application.DTOs.Analytics;

/// <summary>
/// Business analytics DTO
/// </summary>
public record BusinessAnalyticsDto(
    Guid Id,
    Guid BusinessId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    AnalyticsPeriodType PeriodType,

    // Ratings
    decimal AverageRating,
    decimal RatingChange,
    int TotalReviews,
    int NewReviews,

    // Sentiment
    SentimentBreakdownDto Sentiment,

    // Response metrics
    int TotalResponses,
    decimal ResponseRate,
    decimal AverageResponseTimeHours,

    // Engagement
    int HelpfulVotes,
    int ProfileViews,
    int QrCodeScans,

    // Insights
    List<string> TopComplaints,
    List<string> TopPraise,
    Dictionary<string, int> KeywordCloud,

    DateTime CreatedAt
);

/// <summary>
/// Sentiment breakdown DTO
/// </summary>
public record SentimentBreakdownDto(
    int PositiveCount,
    int NeutralCount,
    int NegativeCount,
    decimal PositivePercentage,
    decimal NeutralPercentage,
    decimal NegativePercentage,
    decimal SentimentScore
);

/// <summary>
/// Branch comparison DTO
/// </summary>
public record BranchComparisonDto(
    Guid ParentBusinessId,
    string ParentBusinessName,
    DateTime SnapshotDate,
    List<BranchMetricsDto> Branches,
    BranchMetricsDto? TopPerformer,
    BranchMetricsDto? LowestPerformer,
    decimal AverageRatingAcrossBranches,
    int TotalReviewsAcrossBranches
);

/// <summary>
/// Individual branch metrics
/// </summary>
public record BranchMetricsDto(
    Guid BranchId,
    string BranchName,
    string? Location,
    decimal AverageRating,
    int ReviewCount,
    decimal ResponseRate,
    string? TopComplaint,
    int Rank
);

/// <summary>
/// Competitor comparison DTO
/// </summary>
public record CompetitorComparisonDto(
    Guid BusinessId,
    string BusinessName,
    DateTime SnapshotDate,
    AnalyticsPeriodType PeriodType,
    List<CompetitorMetricsDto> Competitors,
    int RankingPosition,
    decimal AverageRatingDifference
);

/// <summary>
/// Individual competitor metrics
/// </summary>
public record CompetitorMetricsDto(
    Guid CompetitorId,
    string CompetitorName,
    decimal AverageRating,
    int ReviewCount,
    decimal SentimentScore,
    Dictionary<string, int>? TopKeywords,
    int Rank
);

/// <summary>
/// Request to add a competitor for comparison
/// </summary>
public class AddCompetitorRequest
{
    public Guid BusinessId { get; set; }
    public Guid CompetitorBusinessId { get; set; }
}

/// <summary>
/// Request to remove a competitor from comparison
/// </summary>
public class RemoveCompetitorRequest
{
    public Guid BusinessId { get; set; }
    public Guid CompetitorBusinessId { get; set; }
}

/// <summary>
/// Analytics dashboard summary
/// </summary>
public record AnalyticsDashboardDto(
    Guid BusinessId,
    BusinessAnalyticsDto CurrentPeriod,
    BusinessAnalyticsDto? PreviousPeriod,
    TrendSummaryDto Trends,
    List<AlertDto> Alerts
);

/// <summary>
/// Trend summary
/// </summary>
public record TrendSummaryDto(
    decimal RatingTrend,
    decimal ReviewVolumeTrend,
    decimal SentimentTrend,
    decimal ResponseRateTrend,
    string OverallTrendDirection
);

/// <summary>
/// Analytics alert
/// </summary>
public record AlertDto(
    string Type,
    string Message,
    string Severity,
    DateTime CreatedAt
);

/// <summary>
/// Request for analytics data
/// </summary>
public class GetAnalyticsRequest
{
    public Guid BusinessId { get; set; }
    public AnalyticsPeriodType PeriodType { get; set; } = AnalyticsPeriodType.Monthly;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
