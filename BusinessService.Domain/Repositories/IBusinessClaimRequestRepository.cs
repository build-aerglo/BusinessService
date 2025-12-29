using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface IBusinessClaimRequestRepository
{
    Task<BusinessClaimRequest?> FindByIdAsync(Guid id);
    Task<BusinessClaimRequest?> FindPendingByBusinessIdAsync(Guid businessId);
    Task<List<BusinessClaimRequest>> FindByBusinessIdAsync(Guid businessId);
    Task<List<BusinessClaimRequest>> FindPendingAsync();
    Task<List<BusinessClaimRequest>> FindOverdueAsync();
    Task<List<BusinessClaimRequest>> FindByStatusAsync(ClaimRequestStatus status);
    Task AddAsync(BusinessClaimRequest claim);
    Task UpdateAsync(BusinessClaimRequest claim);
    Task<bool> ExistsPendingByBusinessIdAsync(Guid businessId);
    Task<int> CountPendingAsync();
    Task<int> CountOverdueAsync();
}
