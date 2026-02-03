using BusinessService.Application.DTOs.AutoResponse;

namespace BusinessService.Application.Interfaces;

public interface IBusinessAutoResponseService
{
    Task<BusinessAutoResponseDto?> GetByBusinessIdAsync(Guid businessId);
    Task<BusinessAutoResponseDto> UpdateAsync(Guid businessId, UpdateBusinessAutoResponseRequest request);
}
