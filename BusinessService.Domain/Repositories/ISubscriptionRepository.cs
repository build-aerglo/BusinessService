using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> FindByIdAsync(Guid id);
    Task<SubscriptionPlan?> FindByTierAsync(SubscriptionTier tier);
    Task<List<SubscriptionPlan>> GetAllActiveAsync();
    Task AddAsync(SubscriptionPlan plan);
    Task UpdateAsync(SubscriptionPlan plan);
}

public interface IBusinessSubscriptionRepository
{
    Task<BusinessSubscription?> FindByBusinessIdAsync(Guid businessId);
    Task<BusinessSubscription?> FindByIdAsync(Guid id);
    Task<BusinessSubscription?> FindActiveByBusinessIdAsync(Guid businessId);
    Task AddAsync(BusinessSubscription subscription);
    Task UpdateAsync(BusinessSubscription subscription);
    Task<List<BusinessSubscription>> FindExpiringAsync(int daysUntilExpiry);
    Task<List<BusinessSubscription>> FindByStatusAsync(SubscriptionStatus status);
    Task UpdateUsageAsync(Guid subscriptionId, int repliesUsed, int disputesUsed);
}
