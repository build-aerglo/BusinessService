using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface IBusinessAnalyticsRepository
{
    Task<BusinessAnalytics?> FindByIdAsync(Guid id);
    Task<BusinessAnalytics?> FindByBusinessIdAndPeriodAsync(Guid businessId, DateTime periodStart, DateTime periodEnd);
    Task<BusinessAnalytics?> FindLatestByBusinessIdAsync(Guid businessId);
    Task<List<BusinessAnalytics>> FindByBusinessIdAsync(Guid businessId, int limit = 12);
    Task AddAsync(BusinessAnalytics analytics);
    Task UpdateAsync(BusinessAnalytics analytics);
}

public interface IBranchComparisonRepository
{
    Task<BranchComparisonSnapshot?> FindLatestByParentBusinessIdAsync(Guid parentBusinessId);
    Task<List<BranchComparisonSnapshot>> FindByParentBusinessIdAsync(Guid parentBusinessId, int limit = 12);
    Task AddAsync(BranchComparisonSnapshot snapshot);
}

public interface ICompetitorComparisonRepository
{
    Task<CompetitorComparison?> FindByIdAsync(Guid id);
    Task<List<CompetitorComparison>> FindByBusinessIdAsync(Guid businessId);
    Task<List<CompetitorComparison>> FindActiveByBusinessIdAsync(Guid businessId);
    Task AddAsync(CompetitorComparison comparison);
    Task UpdateAsync(CompetitorComparison comparison);
    Task DeleteAsync(Guid id);
    Task<int> CountActiveByBusinessIdAsync(Guid businessId);
    Task<bool> ExistsByBusinessAndCompetitorAsync(Guid businessId, Guid competitorBusinessId);
}

public interface ICompetitorComparisonSnapshotRepository
{
    Task<CompetitorComparisonSnapshot?> FindLatestByBusinessIdAsync(Guid businessId);
    Task<List<CompetitorComparisonSnapshot>> FindByBusinessIdAsync(Guid businessId, int limit = 12);
    Task AddAsync(CompetitorComparisonSnapshot snapshot);
}
