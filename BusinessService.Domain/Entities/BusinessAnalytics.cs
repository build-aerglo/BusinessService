namespace BusinessService.Domain.Entities;

/// <summary>
/// Business analytics for comparison features (Enterprise feature)
/// Tracks metrics for branch and competitor comparison
/// </summary>
public class BusinessAnalytics
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }

    // Time period for analytics
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public AnalyticsPeriodType PeriodType { get; set; }

    // Rating metrics
    public decimal AverageRating { get; set; }
    public decimal RatingChange { get; set; }
    public int TotalReviews { get; set; }
    public int NewReviews { get; set; }

    // Sentiment breakdown
    public int PositiveReviews { get; set; }
    public int NeutralReviews { get; set; }
    public int NegativeReviews { get; set; }
    public decimal SentimentScore { get; set; }

    // Response metrics
    public int TotalResponses { get; set; }
    public decimal ResponseRate { get; set; }
    public decimal AverageResponseTimeHours { get; set; }

    // Engagement metrics
    public int HelpfulVotes { get; set; }
    public int ProfileViews { get; set; }
    public int QrCodeScans { get; set; }

    // Top complaints/praise (JSON array)
    public string? TopComplaintsJson { get; set; }
    public string? TopPraiseJson { get; set; }
    public string? KeywordCloudJson { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Calculates sentiment percentage breakdown
    /// </summary>
    public (decimal positive, decimal neutral, decimal negative) GetSentimentPercentages()
    {
        var total = PositiveReviews + NeutralReviews + NegativeReviews;
        if (total == 0) return (0, 0, 0);

        return (
            Math.Round((decimal)PositiveReviews / total * 100, 2),
            Math.Round((decimal)NeutralReviews / total * 100, 2),
            Math.Round((decimal)NegativeReviews / total * 100, 2)
        );
    }
}

/// <summary>
/// Branch comparison snapshot for multi-location businesses
/// </summary>
public class BranchComparisonSnapshot
{
    public Guid Id { get; set; }
    public Guid ParentBusinessId { get; set; }
    public DateTime SnapshotDate { get; set; }

    // JSON containing comparison data for all branches
    public string? BranchMetricsJson { get; set; }

    // Summary statistics
    public Guid? TopPerformingBranchId { get; set; }
    public Guid? LowestPerformingBranchId { get; set; }
    public decimal AverageRatingAcrossBranches { get; set; }
    public int TotalReviewsAcrossBranches { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Competitor comparison tracking (Enterprise feature)
/// </summary>
public class CompetitorComparison
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid CompetitorBusinessId { get; set; }

    // Competitor details (cached)
    public string CompetitorName { get; set; } = default!;
    public Guid? CompetitorCategoryId { get; set; }

    // Comparison settings
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? AddedByUserId { get; set; }
}

/// <summary>
/// Aggregated competitor comparison snapshot
/// </summary>
public class CompetitorComparisonSnapshot
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public AnalyticsPeriodType PeriodType { get; set; }

    // JSON containing aggregated comparison data
    public string? ComparisonDataJson { get; set; }

    // Summary
    public int CompetitorsCompared { get; set; }
    public int RankingPosition { get; set; }
    public decimal AverageRatingDifference { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Analytics period types
/// </summary>
public enum AnalyticsPeriodType
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4
}
