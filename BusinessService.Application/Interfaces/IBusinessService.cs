using BusinessService.Application.DTOs;
using BusinessService.Domain.Entities;

namespace BusinessService.Application.Interfaces;

public interface IBusinessService
{
    Task<BusinessDto> CreateBusinessAsync(CreateBusinessRequest request);
    Task<BusinessDto> GetBusinessAsync(Guid id);
    Task UpdateRatingsAsync(Guid businessId, decimal newAverage, long newCount);
    Task<BusinessDto> UpdateBusinessAsync(Guid id, UpdateBusinessRequest request);
    
    Task ClaimBusinessAsync(BusinessClaimsDto dto);
    Task<List<BusinessSummaryResponseDto>> GetBusinessesByCategoryAsync(Guid categoryId);
    Task<List<BusinessSummaryResponseDto>> GetBusinessesByTagAsync(Guid tagId);
    
    // branches
    Task<List<BusinessBranches?>> GetBusinessBranchesAsync(Guid businessId);
    Task AddBranchesAsync(BranchDto dto);
    Task DeleteBranchesAsync(Guid id);
    Task<BusinessBranches> UpdateBranchesAsync(BranchUpdateDto dto);

}