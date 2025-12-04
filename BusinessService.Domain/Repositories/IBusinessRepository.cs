using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface IBusinessRepository
{
    Task<bool> ExistsByNameAsync(string name);
    Task<Business?> FindByIdAsync(Guid id);
    Task AddAsync(Business business);
    Task UpdateRatingsAsync(Business business);
    Task UpdateProfileAsync(Business business);
    Task<List<Business>> GetBranchesAsync(Guid parentId);
}