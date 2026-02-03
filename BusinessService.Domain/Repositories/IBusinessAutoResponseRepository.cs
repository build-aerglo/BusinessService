using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface IBusinessAutoResponseRepository
{
    Task<BusinessAutoResponse?> FindByBusinessIdAsync(Guid businessId);
    Task UpdateAsync(BusinessAutoResponse autoResponse);
}
