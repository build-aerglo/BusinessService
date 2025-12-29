using BusinessService.Application.DTOs.Analytics;
using BusinessService.Domain.Entities;

namespace BusinessService.Application.Interfaces;

public interface IBusinessAnalyticsService
{
    // Analytics retrieval
    Task<BusinessAnalyticsDto?> GetLatestAnalyticsAsync(Guid businessId);
    Task<List<BusinessAnalyticsDto>> GetAnalyticsHistoryAsync(Guid businessId, int limit = 12);
    Task<AnalyticsDashboardDto> GetDashboardAsync(Guid businessId);

    // Analytics generation
    Task<BusinessAnalyticsDto> GenerateAnalyticsAsync(Guid businessId, AnalyticsPeriodType periodType);

    // Branch comparison (Enterprise)
    Task<BranchComparisonDto?> GetBranchComparisonAsync(Guid parentBusinessId);
    Task<BranchComparisonDto> GenerateBranchComparisonAsync(Guid parentBusinessId);

    // Competitor comparison (Enterprise)
    Task<CompetitorComparisonDto?> GetCompetitorComparisonAsync(Guid businessId);
    Task AddCompetitorAsync(AddCompetitorRequest request, Guid addedByUserId);
    Task RemoveCompetitorAsync(RemoveCompetitorRequest request);
    Task<CompetitorComparisonDto> GenerateCompetitorComparisonAsync(Guid businessId);

    // Feature availability check
    Task<bool> CanAccessBranchComparisonAsync(Guid businessId);
    Task<bool> CanAccessCompetitorComparisonAsync(Guid businessId);
}
