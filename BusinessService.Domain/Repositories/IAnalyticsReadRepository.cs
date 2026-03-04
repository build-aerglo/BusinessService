using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

/// <summary>
/// Read-only contract for analytics data.
/// The Azure Function is the sole writer; BusinessService only reads.
/// Lives in Domain so Application services can depend on it without touching Infrastructure.
/// </summary>
public interface IAnalyticsReadRepository
{
    /// <summary>
    /// Fetch the pre-calculated analytics dashboard for a business.
    /// Returns null if the Azure Function has not yet processed this business.
    /// </summary>
    Task<BusinessAnalyticsDashboard?> GetDashboardAsync(Guid businessId);
}