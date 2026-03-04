namespace BusinessService.Domain.Entities;

/// <summary>
/// Represents a pre-calculated analytics snapshot for a business.
/// Written exclusively by the Azure Function (AnalyticsProcessorFunction).
/// BusinessService only ever READS from this table.
/// </summary>
public class BusinessAnalyticsDashboard
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public int TotalReviews { get; set; }
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Deserialized from the JSONB metrics column written by the Azure Function.
    /// Null if the Function has not yet processed this business.
    /// </summary>
    public AnalyticsMetrics? Metrics { get; set; }

    public DateTime LastCalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Root metrics object — must exactly mirror what AnalyticsFunction serializes.
/// The function uses camelCase (JsonNamingPolicy.CamelCase).
/// AnalyticsReadRepository deserializes with PropertyNameCaseInsensitive = true
/// so casing doesn't matter, but property names and structure must match.
/// </summary>
public class AnalyticsMetrics
{
    public ResponseMetrics? ResponseMetrics { get; set; }
    public SentimentMetrics? Sentiment { get; set; }
    public TimeSeriesMetrics? TimeSeries { get; set; }
    public Dictionary<string, int>? Sources { get; set; }
    public EngagementMetrics? Engagement { get; set; }
    public TrendMetrics? Trends { get; set; }
}

/// <summary>
/// Written by WrrCalculationService in the AnalyticsFunction.
/// JSONB keys: responseRate, weightedResponseRate, avgResponseTimeHours,
///             totalResponses, positiveReplied, neutralReplied, negativeReplied
/// </summary>
public class ResponseMetrics
{
    public decimal ResponseRate { get; set; }
    public decimal WeightedResponseRate { get; set; }
    public decimal AvgResponseTimeHours { get; set; }
    public int TotalResponses { get; set; }
    public int PositiveReplied { get; set; }
    public int NeutralReplied { get; set; }
    public int NegativeReplied { get; set; }
}

/// <summary>
/// Written by SentimentAnalysisService in the AnalyticsFunction.
/// JSONB keys: positivePct, neutralPct, negativePct, keywords
/// </summary>
public class SentimentMetrics
{
    public decimal PositivePct { get; set; }
    public decimal NeutralPct { get; set; }
    public decimal NegativePct { get; set; }
    public KeywordData? Keywords { get; set; }
}

public class KeywordData
{
    public List<KeywordItem>? Positive { get; set; }
    public List<KeywordItem>? Negative { get; set; }
}

public class KeywordItem
{
    public string Text { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Written by TimeSeriesService in the AnalyticsFunction.
/// JSONB structure: { daily: [...], weekly: [...], monthly: [...] }
/// NOT a flat list — it's an object with three named arrays.
/// </summary>
public class TimeSeriesMetrics
{
    public List<TimeSeriesPoint>? Daily { get; set; }
    public List<TimeSeriesPoint>? Weekly { get; set; }
    public List<TimeSeriesPoint>? Monthly { get; set; }
}

public class TimeSeriesPoint
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal AvgRating { get; set; }
    public decimal SentimentAvg { get; set; }
}

/// <summary>
/// Written by AnalyticsAggregationService.GetEngagementMetricsAsync.
/// JSONB keys: helpfulVotes, profileViews, qrScans
/// </summary>
public class EngagementMetrics
{
    public int HelpfulVotes { get; set; }
    public int ProfileViews { get; set; }
    public int QrScans { get; set; }
}

/// <summary>
/// Written by AnalyticsAggregationService.CalculateTrendsAsync.
/// JSONB keys: reviewsLast30, reviewsPrev30, ratingLast30, ratingPrev30,
///             reviewTrendPct, ratingTrendPct
/// </summary>
public class TrendMetrics
{
    public int ReviewsLast30 { get; set; }
    public int ReviewsPrev30 { get; set; }
    public decimal RatingLast30 { get; set; }
    public decimal RatingPrev30 { get; set; }
    public decimal ReviewTrendPct { get; set; }
    public decimal RatingTrendPct { get; set; }
}