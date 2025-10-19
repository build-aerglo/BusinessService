using BusinessService.Application.DTOs;

namespace BusinessService.Application.Interfaces;

public interface IBusinessService
{
    Task<BusinessDto> CreateBusinessAsync(CreateBusinessRequest request);
    Task<BusinessDto> GetBusinessAsync(Guid id);
    Task UpdateRatingsAsync(Guid businessId, decimal newAverage, long newCount);
}