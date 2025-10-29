using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface IBusinessRepository
{
    Task<bool> ExistsByNameAsync(string name);
    Task<Business?> FindByIdAsync(Guid id);
    Task AddAsync(Business business);
    Task UpdateAsync(Business business);
    
    Task UpdateBusinessDetailsAsync(Business business, List<string>? category);
    Task<List<Business>> GetBranchesAsync(Guid parentId);
}