using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface IBusinessUserRepository
{
    Task<BusinessUser?> FindByIdAsync(Guid id);
    Task<BusinessUser?> FindByEmailAndBusinessIdAsync(string email, Guid businessId);
    Task<BusinessUser?> FindByInvitationTokenAsync(string token);
    Task<List<BusinessUser>> FindByBusinessIdAsync(Guid businessId);
    Task<List<BusinessUser>> FindActiveByBusinessIdAsync(Guid businessId);
    Task<BusinessUser?> FindOwnerByBusinessIdAsync(Guid businessId);
    Task AddAsync(BusinessUser user);
    Task UpdateAsync(BusinessUser user);
    Task<int> CountActiveByBusinessIdAsync(Guid businessId);
    Task<bool> ExistsByEmailAndBusinessIdAsync(string email, Guid businessId);
}
