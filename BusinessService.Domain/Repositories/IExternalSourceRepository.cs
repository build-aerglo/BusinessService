using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface IExternalSourceRepository
{
    Task<ExternalSource?> FindByIdAsync(Guid id);
    Task<List<ExternalSource>> FindByBusinessIdAsync(Guid businessId);
    Task<List<ExternalSource>> FindConnectedByBusinessIdAsync(Guid businessId);
    Task<ExternalSource?> FindByBusinessIdAndTypeAsync(Guid businessId, ExternalSourceType sourceType);
    Task AddAsync(ExternalSource source);
    Task UpdateAsync(ExternalSource source);
    Task DeleteAsync(Guid id);
    Task<int> CountConnectedByBusinessIdAsync(Guid businessId);
    Task<List<ExternalSource>> FindDueSyncAsync();
    Task<bool> ExistsByBusinessIdAndTypeAsync(Guid businessId, ExternalSourceType sourceType);
}
