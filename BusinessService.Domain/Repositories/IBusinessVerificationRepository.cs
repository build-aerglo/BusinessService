using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface IBusinessVerificationRepository
{
    Task<BusinessVerification?> FindByBusinessIdAsync(Guid businessId);
    Task<BusinessVerification?> FindByIdAsync(Guid id);
    Task AddAsync(BusinessVerification verification);
    Task UpdateAsync(BusinessVerification verification);
    Task<bool> ExistsByBusinessIdAsync(Guid businessId);
    Task<List<BusinessVerification>> FindByVerificationLevelAsync(VerificationLevel level);
    Task<List<BusinessVerification>> FindRequiringReverificationAsync();
    Task UpdateIdVerificationStatusAsync(Guid businessId, bool idVerified, string idVerificationStatus);
    Task UpdatePhoneAndEmailVerificationAsync(Guid businessId, bool phoneVerified, bool emailVerified, string? phoneNumber);
}
