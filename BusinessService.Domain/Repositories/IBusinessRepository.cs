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
    
    Task ClaimAsync(BusinessClaims claim);
    Task<List<Business>> GetBusinessesByCategoryAsync(Guid categoryId);
    Task<List<Business>> GetBusinessesByCategoryIdAsync(Guid categoryId);
    
    Task <List<BusinessBranches?>> GetBusinessBranchesAsync(Guid parentId);
    Task AddBusinessBranchAsync(BusinessBranches branch);
    Task DeleteBusinessBranchAsync(Guid id);
    Task UpdateBusinessBranchAsync(BusinessBranches branch);
    Task<BusinessBranches?> FindBranchByIdAsync(Guid id);
    Task UpdateIdVerificationAsync(Guid businessId, string idVerificationUrl, string idVerificationType, string idVerificationNumber);
    Task UpdatePreferredContactMethodAsync(Guid businessId, string preferredContactMethod);
    Task<Guid?> GetBusinessUserIdByBusinessIdAsync(Guid businessId);
}