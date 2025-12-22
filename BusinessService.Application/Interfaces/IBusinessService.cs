using BusinessService.Application.DTOs;

namespace BusinessService.Application.Interfaces;

public interface IBusinessService
{
    Task<BusinessDto> CreateBusinessAsync(CreateBusinessRequest request);
    Task<BusinessDto> GetBusinessAsync(Guid id);
    Task UpdateRatingsAsync(Guid businessId, decimal newAverage, long newCount);
    Task<BusinessDto> UpdateBusinessAsync(Guid id, UpdateBusinessRequest request);
    Task<List<BusinessSummaryDto>> GetBusinessesByCategoryAsync(Guid categoryId);
    Task<List<BusinessDto>> GetBusinessesByTagAsync(Guid tagId);

}