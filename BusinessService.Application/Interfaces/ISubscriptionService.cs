using BusinessService.Application.DTOs.Subscription;
using BusinessService.Domain.Entities;

namespace BusinessService.Application.Interfaces;

public interface ISubscriptionService
{
    // Plan management
    Task<List<SubscriptionPlanDto>> GetAllPlansAsync();
    Task<SubscriptionPlanDto?> GetPlanByIdAsync(Guid planId);
    Task<SubscriptionPlanDto?> GetPlanByTierAsync(SubscriptionTier tier);

    // Subscription management
    Task<BusinessSubscriptionDto?> GetBusinessSubscriptionAsync(Guid businessId);
    Task<BusinessSubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request);
    Task<BusinessSubscriptionDto> UpgradeSubscriptionAsync(UpgradeSubscriptionRequest request);
    Task CancelSubscriptionAsync(CancelSubscriptionRequest request);

    // Usage tracking
    Task<SubscriptionUsageDto> GetUsageAsync(Guid businessId);
    Task<bool> CanPerformActionAsync(Guid businessId, string actionType);
    Task IncrementUsageAsync(Guid businessId, string actionType);

    // Feature checks
    Task<FeatureAvailabilityResponse> CheckFeatureAvailabilityAsync(Guid businessId, string featureName);
    Task<SubscriptionComparisonDto?> GetUpgradeComparisonAsync(Guid businessId);

    // Subscription lifecycle
    Task<List<BusinessSubscriptionDto>> GetExpiringSubscriptionsAsync(int daysUntilExpiry);
    Task ProcessExpiredSubscriptionsAsync();
}
