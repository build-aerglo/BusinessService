using BusinessService.Domain.Entities;

namespace BusinessService.Application.DTOs.Subscription;

/// <summary>
/// Subscription plan details DTO
/// </summary>
public record SubscriptionPlanDto(
    Guid Id,
    string Name,
    SubscriptionTier Tier,
    string? Description,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    string Currency,

    // Limits
    int MonthlyReplyLimit,
    int MonthlyDisputeLimit,
    int ExternalSourceLimit,
    int UserLoginLimit,

    // Features
    bool PrivateReviewsEnabled,
    bool DataApiEnabled,
    bool DndModeEnabled,
    bool AutoResponseEnabled,
    bool BranchComparisonEnabled,
    bool CompetitorComparisonEnabled,

    bool IsActive
);

/// <summary>
/// Business subscription details DTO
/// </summary>
public record BusinessSubscriptionDto(
    Guid Id,
    Guid BusinessId,
    Guid SubscriptionPlanId,
    string PlanName,
    SubscriptionTier Tier,

    // Period
    DateTime StartDate,
    DateTime EndDate,
    bool IsAnnual,
    int DaysRemaining,

    // Status
    SubscriptionStatus Status,
    bool IsActive,

    // Usage
    SubscriptionUsageDto Usage,

    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Subscription usage tracking DTO
/// </summary>
public record SubscriptionUsageDto(
    int RepliesUsed,
    int RepliesLimit,
    int RepliesRemaining,
    decimal RepliesPercentage,

    int DisputesUsed,
    int DisputesLimit,
    int DisputesRemaining,
    decimal DisputesPercentage,

    DateTime UsageResetDate
);

/// <summary>
/// Request to create/update subscription
/// </summary>
public class CreateSubscriptionRequest
{
    public Guid BusinessId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public bool IsAnnual { get; set; }
    public string? PaymentReference { get; set; }
}

/// <summary>
/// Request to upgrade subscription
/// </summary>
public class UpgradeSubscriptionRequest
{
    public Guid BusinessId { get; set; }
    public Guid NewPlanId { get; set; }
    public bool IsAnnual { get; set; }
    public string? PaymentReference { get; set; }
}

/// <summary>
/// Request to cancel subscription
/// </summary>
public class CancelSubscriptionRequest
{
    public Guid BusinessId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Feature availability check response
/// </summary>
public record FeatureAvailabilityResponse(
    string FeatureName,
    bool IsAvailable,
    SubscriptionTier RequiredTier,
    string? Message
);

/// <summary>
/// Subscription comparison for upgrade prompt
/// </summary>
public record SubscriptionComparisonDto(
    SubscriptionPlanDto CurrentPlan,
    SubscriptionPlanDto RecommendedPlan,
    List<string> AdditionalFeatures,
    decimal PriceDifference
);
