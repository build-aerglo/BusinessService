using BusinessService.Application.DTOs.Subscription;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;

namespace BusinessService.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IBusinessSubscriptionRepository _subscriptionRepository;
    private readonly IBusinessRepository _businessRepository;

    public SubscriptionService(
        ISubscriptionPlanRepository planRepository,
        IBusinessSubscriptionRepository subscriptionRepository,
        IBusinessRepository businessRepository)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _businessRepository = businessRepository;
    }

    public async Task<List<SubscriptionPlanDto>> GetAllPlansAsync()
    {
        var plans = await _planRepository.GetAllActiveAsync();
        return plans.Select(MapPlanToDto).ToList();
    }

    public async Task<SubscriptionPlanDto?> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _planRepository.FindByIdAsync(planId);
        return plan != null ? MapPlanToDto(plan) : null;
    }

    public async Task<SubscriptionPlanDto?> GetPlanByTierAsync(SubscriptionTier tier)
    {
        var plan = await _planRepository.FindByTierAsync(tier);
        return plan != null ? MapPlanToDto(plan) : null;
    }

    public async Task<BusinessSubscriptionDto?> GetBusinessSubscriptionAsync(Guid businessId)
    {
        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(businessId);
        if (subscription == null) return null;

        var plan = await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId);
        return MapSubscriptionToDto(subscription, plan);
    }

    public async Task<BusinessSubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request)
    {
        var business = await _businessRepository.FindByIdAsync(request.BusinessId);
        if (business == null)
            throw new BusinessNotFoundException($"Business with ID {request.BusinessId} not found");

        var plan = await _planRepository.FindByIdAsync(request.SubscriptionPlanId);
        if (plan == null)
            throw new SubscriptionNotFoundException($"Subscription plan with ID {request.SubscriptionPlanId} not found");

        var existingSubscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(request.BusinessId);
        if (existingSubscription != null)
            throw new InvalidSubscriptionOperationException("Business already has an active subscription");

        var subscription = new BusinessSubscription
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            SubscriptionPlanId = request.SubscriptionPlanId,
            StartDate = DateTime.UtcNow,
            EndDate = request.IsAnnual ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1),
            BillingDate = DateTime.UtcNow,
            IsAnnual = request.IsAnnual,
            Status = SubscriptionStatus.Active,
            UsageResetDate = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _subscriptionRepository.AddAsync(subscription);
        return MapSubscriptionToDto(subscription, plan);
    }

    public async Task<BusinessSubscriptionDto> UpgradeSubscriptionAsync(UpgradeSubscriptionRequest request)
    {
        var currentSubscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(request.BusinessId);
        if (currentSubscription == null)
            throw new SubscriptionNotFoundException("No active subscription found");

        var newPlan = await _planRepository.FindByIdAsync(request.NewPlanId);
        if (newPlan == null)
            throw new SubscriptionNotFoundException($"Subscription plan with ID {request.NewPlanId} not found");

        currentSubscription.SubscriptionPlanId = request.NewPlanId;
        currentSubscription.IsAnnual = request.IsAnnual;
        currentSubscription.EndDate = request.IsAnnual
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);
        currentSubscription.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(currentSubscription);
        return MapSubscriptionToDto(currentSubscription, newPlan);
    }

    public async Task CancelSubscriptionAsync(CancelSubscriptionRequest request)
    {
        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(request.BusinessId);
        if (subscription == null)
            throw new SubscriptionNotFoundException("No active subscription found");

        subscription.Cancel(request.Reason);
        await _subscriptionRepository.UpdateAsync(subscription);
    }

    public async Task<SubscriptionUsageDto> GetUsageAsync(Guid businessId)
    {
        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(businessId);
        if (subscription == null)
        {
            return new SubscriptionUsageDto(0, 10, 10, 0, 0, 5, 5, 0, DateTime.UtcNow.AddMonths(1));
        }

        var plan = await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId);
        if (plan == null)
            throw new SubscriptionNotFoundException("Plan not found for subscription");

        var repliesRemaining = plan.MonthlyReplyLimit == int.MaxValue
            ? int.MaxValue
            : Math.Max(0, plan.MonthlyReplyLimit - subscription.RepliesUsedThisMonth);

        var disputesRemaining = plan.MonthlyDisputeLimit == int.MaxValue
            ? int.MaxValue
            : Math.Max(0, plan.MonthlyDisputeLimit - subscription.DisputesUsedThisMonth);

        var repliesPercentage = plan.MonthlyReplyLimit == int.MaxValue
            ? 0
            : Math.Round((decimal)subscription.RepliesUsedThisMonth / plan.MonthlyReplyLimit * 100, 2);

        var disputesPercentage = plan.MonthlyDisputeLimit == int.MaxValue
            ? 0
            : Math.Round((decimal)subscription.DisputesUsedThisMonth / plan.MonthlyDisputeLimit * 100, 2);

        return new SubscriptionUsageDto(
            subscription.RepliesUsedThisMonth,
            plan.MonthlyReplyLimit,
            repliesRemaining,
            repliesPercentage,
            subscription.DisputesUsedThisMonth,
            plan.MonthlyDisputeLimit,
            disputesRemaining,
            disputesPercentage,
            subscription.UsageResetDate
        );
    }

    public async Task<bool> CanPerformActionAsync(Guid businessId, string actionType)
    {
        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(businessId);
        if (subscription == null)
        {
            var basicPlan = await _planRepository.FindByTierAsync(SubscriptionTier.Basic);
            if (basicPlan == null) return false;

            return actionType switch
            {
                "reply" => true,
                "dispute" => true,
                _ => false
            };
        }

        var plan = await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId);
        if (plan == null) return false;

        return actionType switch
        {
            "reply" => subscription.CanReply(plan.MonthlyReplyLimit),
            "dispute" => subscription.CanDispute(plan.MonthlyDisputeLimit),
            "private_reviews" => plan.PrivateReviewsEnabled,
            "dnd_mode" => plan.DndModeEnabled,
            "auto_response" => plan.AutoResponseEnabled,
            "data_api" => plan.DataApiEnabled,
            "branch_comparison" => plan.BranchComparisonEnabled,
            "competitor_comparison" => plan.CompetitorComparisonEnabled,
            _ => false
        };
    }

    public async Task IncrementUsageAsync(Guid businessId, string actionType)
    {
        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(businessId);
        if (subscription == null) return;

        switch (actionType)
        {
            case "reply":
                subscription.IncrementReplyUsage();
                break;
            case "dispute":
                subscription.IncrementDisputeUsage();
                break;
        }

        await _subscriptionRepository.UpdateAsync(subscription);
    }

    public async Task<FeatureAvailabilityResponse> CheckFeatureAvailabilityAsync(Guid businessId, string featureName)
    {
        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(businessId);
        SubscriptionPlan? plan = null;

        if (subscription != null)
            plan = await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId);
        else
            plan = await _planRepository.FindByTierAsync(SubscriptionTier.Basic);

        if (plan == null)
            return new FeatureAvailabilityResponse(featureName, false, SubscriptionTier.Basic, "No plan found");

        var (isAvailable, requiredTier) = featureName switch
        {
            "private_reviews" => (plan.PrivateReviewsEnabled, SubscriptionTier.Premium),
            "dnd_mode" => (plan.DndModeEnabled, SubscriptionTier.Enterprise),
            "auto_response" => (plan.AutoResponseEnabled, SubscriptionTier.Enterprise),
            "data_api" => (plan.DataApiEnabled, SubscriptionTier.Enterprise),
            "branch_comparison" => (plan.BranchComparisonEnabled, SubscriptionTier.Enterprise),
            "competitor_comparison" => (plan.CompetitorComparisonEnabled, SubscriptionTier.Enterprise),
            _ => (false, SubscriptionTier.Enterprise)
        };

        var message = isAvailable
            ? null
            : $"Upgrade to {requiredTier} to access this feature";

        return new FeatureAvailabilityResponse(featureName, isAvailable, requiredTier, message);
    }

    public async Task<SubscriptionComparisonDto?> GetUpgradeComparisonAsync(Guid businessId)
    {
        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(businessId);
        SubscriptionPlan? currentPlan;

        if (subscription != null)
            currentPlan = await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId);
        else
            currentPlan = await _planRepository.FindByTierAsync(SubscriptionTier.Basic);

        if (currentPlan == null) return null;

        var nextTier = currentPlan.Tier switch
        {
            SubscriptionTier.Basic => SubscriptionTier.Premium,
            SubscriptionTier.Premium => SubscriptionTier.Enterprise,
            _ => (SubscriptionTier?)null
        };

        if (nextTier == null) return null;

        var recommendedPlan = await _planRepository.FindByTierAsync(nextTier.Value);
        if (recommendedPlan == null) return null;

        var additionalFeatures = new List<string>();
        if (!currentPlan.PrivateReviewsEnabled && recommendedPlan.PrivateReviewsEnabled)
            additionalFeatures.Add("Private Reviews");
        if (!currentPlan.DndModeEnabled && recommendedPlan.DndModeEnabled)
            additionalFeatures.Add("Do Not Disturb Mode");
        if (!currentPlan.AutoResponseEnabled && recommendedPlan.AutoResponseEnabled)
            additionalFeatures.Add("Auto-Response Templates");
        if (!currentPlan.DataApiEnabled && recommendedPlan.DataApiEnabled)
            additionalFeatures.Add("Data API Access");
        if (!currentPlan.BranchComparisonEnabled && recommendedPlan.BranchComparisonEnabled)
            additionalFeatures.Add("Branch Comparison Analytics");
        if (!currentPlan.CompetitorComparisonEnabled && recommendedPlan.CompetitorComparisonEnabled)
            additionalFeatures.Add("Competitor Comparison Analytics");

        var priceDifference = recommendedPlan.MonthlyPrice - currentPlan.MonthlyPrice;

        return new SubscriptionComparisonDto(
            MapPlanToDto(currentPlan),
            MapPlanToDto(recommendedPlan),
            additionalFeatures,
            priceDifference
        );
    }

    public async Task<List<BusinessSubscriptionDto>> GetExpiringSubscriptionsAsync(int daysUntilExpiry)
    {
        var subscriptions = await _subscriptionRepository.FindExpiringAsync(daysUntilExpiry);
        var result = new List<BusinessSubscriptionDto>();

        foreach (var subscription in subscriptions)
        {
            var plan = await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId);
            result.Add(MapSubscriptionToDto(subscription, plan));
        }

        return result;
    }

    public async Task ProcessExpiredSubscriptionsAsync()
    {
        var activeSubscriptions = await _subscriptionRepository.FindByStatusAsync(SubscriptionStatus.Active);

        foreach (var subscription in activeSubscriptions)
        {
            if (DateTime.UtcNow >= subscription.EndDate)
            {
                subscription.Status = SubscriptionStatus.Expired;
                subscription.UpdatedAt = DateTime.UtcNow;
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }
    }

    private static SubscriptionPlanDto MapPlanToDto(SubscriptionPlan plan)
    {
        return new SubscriptionPlanDto(
            plan.Id,
            plan.Name,
            plan.Tier,
            plan.Description,
            plan.MonthlyPrice,
            plan.AnnualPrice,
            plan.Currency,
            plan.MonthlyReplyLimit,
            plan.MonthlyDisputeLimit,
            plan.ExternalSourceLimit,
            plan.UserLoginLimit,
            plan.PrivateReviewsEnabled,
            plan.DataApiEnabled,
            plan.DndModeEnabled,
            plan.AutoResponseEnabled,
            plan.BranchComparisonEnabled,
            plan.CompetitorComparisonEnabled,
            plan.IsActive
        );
    }

    private static BusinessSubscriptionDto MapSubscriptionToDto(BusinessSubscription subscription, SubscriptionPlan? plan)
    {
        var daysRemaining = (int)Math.Max(0, (subscription.EndDate - DateTime.UtcNow).TotalDays);

        var usage = new SubscriptionUsageDto(
            subscription.RepliesUsedThisMonth,
            plan?.MonthlyReplyLimit ?? 10,
            plan?.MonthlyReplyLimit == int.MaxValue ? int.MaxValue : Math.Max(0, (plan?.MonthlyReplyLimit ?? 10) - subscription.RepliesUsedThisMonth),
            plan?.MonthlyReplyLimit == int.MaxValue ? 0 : Math.Round((decimal)subscription.RepliesUsedThisMonth / (plan?.MonthlyReplyLimit ?? 10) * 100, 2),
            subscription.DisputesUsedThisMonth,
            plan?.MonthlyDisputeLimit ?? 5,
            plan?.MonthlyDisputeLimit == int.MaxValue ? int.MaxValue : Math.Max(0, (plan?.MonthlyDisputeLimit ?? 5) - subscription.DisputesUsedThisMonth),
            plan?.MonthlyDisputeLimit == int.MaxValue ? 0 : Math.Round((decimal)subscription.DisputesUsedThisMonth / (plan?.MonthlyDisputeLimit ?? 5) * 100, 2),
            subscription.UsageResetDate
        );

        return new BusinessSubscriptionDto(
            subscription.Id,
            subscription.BusinessId,
            subscription.SubscriptionPlanId,
            plan?.Name ?? "Unknown",
            plan?.Tier ?? SubscriptionTier.Basic,
            subscription.StartDate,
            subscription.EndDate,
            subscription.IsAnnual,
            daysRemaining,
            subscription.Status,
            subscription.IsActive,
            usage,
            subscription.CreatedAt,
            subscription.UpdatedAt
        );
    }
}
