using BusinessService.Application.DTOs.Analytics;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using Newtonsoft.Json;

namespace BusinessService.Application.Services;

public class BusinessAnalyticsService : IBusinessAnalyticsService
{
    private readonly IBusinessAnalyticsRepository _analyticsRepository;
    private readonly IBranchComparisonRepository _branchComparisonRepository;
    private readonly ICompetitorComparisonRepository _competitorComparisonRepository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IBusinessSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;

    public BusinessAnalyticsService(
        IBusinessAnalyticsRepository analyticsRepository,
        IBranchComparisonRepository branchComparisonRepository,
        ICompetitorComparisonRepository competitorComparisonRepository,
        IBusinessRepository businessRepository,
        IBusinessSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository)
    {
        _analyticsRepository = analyticsRepository;
        _branchComparisonRepository = branchComparisonRepository;
        _competitorComparisonRepository = competitorComparisonRepository;
        _businessRepository = businessRepository;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
    }

    public async Task<BusinessAnalyticsDto?> GetLatestAnalyticsAsync(Guid businessId)
    {
        var analytics = await _analyticsRepository.FindLatestByBusinessIdAsync(businessId);
        return analytics != null ? MapToDto(analytics) : null;
    }

    public async Task<List<BusinessAnalyticsDto>> GetAnalyticsHistoryAsync(Guid businessId, int limit = 12)
    {
        var analyticsHistory = await _analyticsRepository.FindByBusinessIdAsync(businessId, limit);
        return analyticsHistory.Select(MapToDto).ToList();
    }

    public async Task<AnalyticsDashboardDto> GetDashboardAsync(Guid businessId)
    {
        var currentPeriod = await _analyticsRepository.FindLatestByBusinessIdAsync(businessId);
        if (currentPeriod == null)
        {
            currentPeriod = await GenerateAnalyticsEntityAsync(businessId, AnalyticsPeriodType.Monthly);
        }

        var history = await _analyticsRepository.FindByBusinessIdAsync(businessId, 2);
        var previousPeriod = history.Count > 1 ? history[1] : null;

        var trends = CalculateTrends(currentPeriod, previousPeriod);
        var alerts = GenerateAlerts(currentPeriod);

        return new AnalyticsDashboardDto(
            businessId,
            MapToDto(currentPeriod),
            previousPeriod != null ? MapToDto(previousPeriod) : null,
            trends,
            alerts
        );
    }

    public async Task<BusinessAnalyticsDto> GenerateAnalyticsAsync(Guid businessId, AnalyticsPeriodType periodType)
    {
        var analytics = await GenerateAnalyticsEntityAsync(businessId, periodType);
        return MapToDto(analytics);
    }

    private async Task<BusinessAnalytics> GenerateAnalyticsEntityAsync(Guid businessId, AnalyticsPeriodType periodType)
    {
        var business = await _businessRepository.FindByIdAsync(businessId);
        if (business == null)
            throw new BusinessNotFoundException($"Business with ID {businessId} not found");

        var (periodStart, periodEnd) = GetPeriodDates(periodType);

        var existing = await _analyticsRepository.FindByBusinessIdAndPeriodAsync(businessId, periodStart, periodEnd);
        if (existing != null)
            return existing;

        // In a real implementation, this would aggregate data from reviews
        // For now, we'll use the business's current stats
        var analytics = new BusinessAnalytics
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            PeriodType = periodType,
            AverageRating = business.AvgRating,
            RatingChange = 0,
            TotalReviews = (int)business.ReviewCount,
            NewReviews = 0,
            PositiveReviews = 0,
            NeutralReviews = 0,
            NegativeReviews = 0,
            SentimentScore = 0.5m,
            TotalResponses = 0,
            ResponseRate = 0,
            AverageResponseTimeHours = 0,
            HelpfulVotes = 0,
            ProfileViews = (int)business.ProfileClicks,
            QrCodeScans = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _analyticsRepository.AddAsync(analytics);
        return analytics;
    }

    public async Task<BranchComparisonDto?> GetBranchComparisonAsync(Guid parentBusinessId)
    {
        if (!await CanAccessBranchComparisonAsync(parentBusinessId))
            throw new FeatureNotAvailableException("Branch Comparison", "Enterprise");

        var snapshot = await _branchComparisonRepository.FindLatestByParentBusinessIdAsync(parentBusinessId);
        if (snapshot == null) return null;

        var parentBusiness = await _businessRepository.FindByIdAsync(parentBusinessId);
        var branchMetrics = ParseBranchMetrics(snapshot.BranchMetricsJson);

        BranchMetricsDto? topPerformer = null;
        BranchMetricsDto? lowestPerformer = null;

        if (snapshot.TopPerformingBranchId.HasValue)
        {
            topPerformer = branchMetrics.FirstOrDefault(b => b.BranchId == snapshot.TopPerformingBranchId);
        }
        if (snapshot.LowestPerformingBranchId.HasValue)
        {
            lowestPerformer = branchMetrics.FirstOrDefault(b => b.BranchId == snapshot.LowestPerformingBranchId);
        }

        return new BranchComparisonDto(
            parentBusinessId,
            parentBusiness?.Name ?? "Unknown",
            snapshot.SnapshotDate,
            branchMetrics,
            topPerformer,
            lowestPerformer,
            snapshot.AverageRatingAcrossBranches,
            snapshot.TotalReviewsAcrossBranches
        );
    }

    public async Task<BranchComparisonDto> GenerateBranchComparisonAsync(Guid parentBusinessId)
    {
        if (!await CanAccessBranchComparisonAsync(parentBusinessId))
            throw new FeatureNotAvailableException("Branch Comparison", "Enterprise");

        var parentBusiness = await _businessRepository.FindByIdAsync(parentBusinessId);
        if (parentBusiness == null)
            throw new BusinessNotFoundException($"Business with ID {parentBusinessId} not found");

        var branches = await _businessRepository.GetBranchesAsync(parentBusinessId);
        var branchMetrics = new List<BranchMetricsDto>();
        var rank = 1;

        foreach (var branch in branches.OrderByDescending(b => b.AvgRating))
        {
            branchMetrics.Add(new BranchMetricsDto(
                branch.Id,
                branch.Name,
                branch.BusinessAddress,
                branch.AvgRating,
                (int)branch.ReviewCount,
                0,
                null,
                rank++
            ));
        }

        var topPerformer = branchMetrics.FirstOrDefault();
        var lowestPerformer = branchMetrics.LastOrDefault();

        var snapshot = new BranchComparisonSnapshot
        {
            Id = Guid.NewGuid(),
            ParentBusinessId = parentBusinessId,
            SnapshotDate = DateTime.UtcNow,
            BranchMetricsJson = JsonConvert.SerializeObject(branchMetrics),
            TopPerformingBranchId = topPerformer?.BranchId,
            LowestPerformingBranchId = lowestPerformer?.BranchId,
            AverageRatingAcrossBranches = branchMetrics.Any() ? branchMetrics.Average(b => b.AverageRating) : 0,
            TotalReviewsAcrossBranches = branchMetrics.Sum(b => b.ReviewCount),
            CreatedAt = DateTime.UtcNow
        };

        await _branchComparisonRepository.AddAsync(snapshot);

        return new BranchComparisonDto(
            parentBusinessId,
            parentBusiness.Name,
            snapshot.SnapshotDate,
            branchMetrics,
            topPerformer,
            lowestPerformer,
            snapshot.AverageRatingAcrossBranches,
            snapshot.TotalReviewsAcrossBranches
        );
    }

    public async Task<CompetitorComparisonDto?> GetCompetitorComparisonAsync(Guid businessId)
    {
        if (!await CanAccessCompetitorComparisonAsync(businessId))
            throw new FeatureNotAvailableException("Competitor Comparison", "Enterprise");

        var competitors = await _competitorComparisonRepository.FindActiveByBusinessIdAsync(businessId);
        if (!competitors.Any()) return null;

        var business = await _businessRepository.FindByIdAsync(businessId);
        var competitorMetrics = new List<CompetitorMetricsDto>();
        var rank = 1;

        foreach (var competitor in competitors)
        {
            var competitorBusiness = await _businessRepository.FindByIdAsync(competitor.CompetitorBusinessId);
            if (competitorBusiness != null)
            {
                competitorMetrics.Add(new CompetitorMetricsDto(
                    competitor.CompetitorBusinessId,
                    competitorBusiness.Name,
                    competitorBusiness.AvgRating,
                    (int)competitorBusiness.ReviewCount,
                    0,
                    null,
                    rank++
                ));
            }
        }

        var avgRatingDiff = business != null && competitorMetrics.Any()
            ? business.AvgRating - competitorMetrics.Average(c => c.AverageRating)
            : 0;

        return new CompetitorComparisonDto(
            businessId,
            business?.Name ?? "Unknown",
            DateTime.UtcNow,
            AnalyticsPeriodType.Monthly,
            competitorMetrics,
            1,
            avgRatingDiff
        );
    }

    public async Task AddCompetitorAsync(AddCompetitorRequest request, Guid addedByUserId)
    {
        if (!await CanAccessCompetitorComparisonAsync(request.BusinessId))
            throw new FeatureNotAvailableException("Competitor Comparison", "Enterprise");

        if (await _competitorComparisonRepository.ExistsByBusinessAndCompetitorAsync(request.BusinessId, request.CompetitorBusinessId))
            throw new InvalidSubscriptionOperationException("This competitor is already added");

        var competitorBusiness = await _businessRepository.FindByIdAsync(request.CompetitorBusinessId);
        if (competitorBusiness == null)
            throw new BusinessNotFoundException($"Competitor business with ID {request.CompetitorBusinessId} not found");

        var existingCount = await _competitorComparisonRepository.CountActiveByBusinessIdAsync(request.BusinessId);
        if (existingCount >= 3)
            throw new SubscriptionLimitExceededException("Competitor comparison limit", existingCount, 3);

        var comparison = new CompetitorComparison
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            CompetitorBusinessId = request.CompetitorBusinessId,
            CompetitorName = competitorBusiness.Name,
            IsActive = true,
            DisplayOrder = existingCount + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AddedByUserId = addedByUserId
        };

        await _competitorComparisonRepository.AddAsync(comparison);
    }

    public async Task RemoveCompetitorAsync(RemoveCompetitorRequest request)
    {
        var comparison = await _competitorComparisonRepository.FindByIdAsync(request.CompetitorBusinessId);
        if (comparison == null || comparison.BusinessId != request.BusinessId)
            throw new BusinessNotFoundException("Competitor comparison not found");

        await _competitorComparisonRepository.DeleteAsync(comparison.Id);
    }

    public async Task<CompetitorComparisonDto> GenerateCompetitorComparisonAsync(Guid businessId)
    {
        var result = await GetCompetitorComparisonAsync(businessId);
        if (result == null)
            throw new BusinessNotFoundException("No competitors configured for comparison");

        return result;
    }

    public async Task<bool> CanAccessBranchComparisonAsync(Guid businessId)
    {
        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(businessId);
        if (subscription == null) return false;

        var plan = await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId);
        return plan?.BranchComparisonEnabled ?? false;
    }

    public async Task<bool> CanAccessCompetitorComparisonAsync(Guid businessId)
    {
        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(businessId);
        if (subscription == null) return false;

        var plan = await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId);
        return plan?.CompetitorComparisonEnabled ?? false;
    }

    private static (DateTime start, DateTime end) GetPeriodDates(AnalyticsPeriodType periodType)
    {
        var now = DateTime.UtcNow;
        return periodType switch
        {
            AnalyticsPeriodType.Daily => (now.Date.AddDays(-1), now.Date),
            AnalyticsPeriodType.Weekly => (now.Date.AddDays(-7), now.Date),
            AnalyticsPeriodType.Monthly => (now.Date.AddMonths(-1), now.Date),
            AnalyticsPeriodType.Quarterly => (now.Date.AddMonths(-3), now.Date),
            AnalyticsPeriodType.Yearly => (now.Date.AddYears(-1), now.Date),
            _ => (now.Date.AddMonths(-1), now.Date)
        };
    }

    private static TrendSummaryDto CalculateTrends(BusinessAnalytics current, BusinessAnalytics? previous)
    {
        if (previous == null)
        {
            return new TrendSummaryDto(0, 0, 0, 0, "stable");
        }

        var ratingTrend = current.AverageRating - previous.AverageRating;
        var volumeTrend = previous.NewReviews > 0
            ? ((decimal)current.NewReviews - previous.NewReviews) / previous.NewReviews * 100
            : 0;
        var sentimentTrend = current.SentimentScore - previous.SentimentScore;
        var responseTrend = current.ResponseRate - previous.ResponseRate;

        var overallTrend = ratingTrend > 0 ? "improving" : ratingTrend < 0 ? "declining" : "stable";

        return new TrendSummaryDto(ratingTrend, volumeTrend, sentimentTrend, responseTrend, overallTrend);
    }

    private static List<AlertDto> GenerateAlerts(BusinessAnalytics analytics)
    {
        var alerts = new List<AlertDto>();

        if (analytics.NegativeReviews > 5)
        {
            alerts.Add(new AlertDto(
                "negative_reviews",
                $"{analytics.NegativeReviews} negative reviews this period",
                "warning",
                DateTime.UtcNow
            ));
        }

        if (analytics.ResponseRate < 0.5m)
        {
            alerts.Add(new AlertDto(
                "low_response_rate",
                "Response rate is below 50%",
                "info",
                DateTime.UtcNow
            ));
        }

        return alerts;
    }

    private static List<BranchMetricsDto> ParseBranchMetrics(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new List<BranchMetricsDto>();
        try
        {
            return JsonConvert.DeserializeObject<List<BranchMetricsDto>>(json) ?? new List<BranchMetricsDto>();
        }
        catch
        {
            return new List<BranchMetricsDto>();
        }
    }

    private static BusinessAnalyticsDto MapToDto(BusinessAnalytics analytics)
    {
        var (posPercent, neutPercent, negPercent) = analytics.GetSentimentPercentages();

        var sentiment = new SentimentBreakdownDto(
            analytics.PositiveReviews,
            analytics.NeutralReviews,
            analytics.NegativeReviews,
            posPercent,
            neutPercent,
            negPercent,
            analytics.SentimentScore
        );

        var topComplaints = !string.IsNullOrEmpty(analytics.TopComplaintsJson)
            ? JsonConvert.DeserializeObject<List<string>>(analytics.TopComplaintsJson) ?? new List<string>()
            : new List<string>();

        var topPraise = !string.IsNullOrEmpty(analytics.TopPraiseJson)
            ? JsonConvert.DeserializeObject<List<string>>(analytics.TopPraiseJson) ?? new List<string>()
            : new List<string>();

        var keywordCloud = !string.IsNullOrEmpty(analytics.KeywordCloudJson)
            ? JsonConvert.DeserializeObject<Dictionary<string, int>>(analytics.KeywordCloudJson) ?? new Dictionary<string, int>()
            : new Dictionary<string, int>();

        return new BusinessAnalyticsDto(
            analytics.Id,
            analytics.BusinessId,
            analytics.PeriodStart,
            analytics.PeriodEnd,
            analytics.PeriodType,
            analytics.AverageRating,
            analytics.RatingChange,
            analytics.TotalReviews,
            analytics.NewReviews,
            sentiment,
            analytics.TotalResponses,
            analytics.ResponseRate,
            analytics.AverageResponseTimeHours,
            analytics.HelpfulVotes,
            analytics.ProfileViews,
            analytics.QrCodeScans,
            topComplaints,
            topPraise,
            keywordCloud,
            analytics.CreatedAt
        );
    }
}
