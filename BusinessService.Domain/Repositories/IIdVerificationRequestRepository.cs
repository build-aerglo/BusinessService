using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface IIdVerificationRequestRepository
{
    Task<IdVerificationRequest?> FindByIdAsync(Guid id);
    Task<IdVerificationRequest?> FindByBusinessIdAsync(Guid businessId);
    Task<List<IdVerificationRequest>> FindAllByBusinessIdAsync(Guid businessId);
    Task AddAsync(IdVerificationRequest request);
}
