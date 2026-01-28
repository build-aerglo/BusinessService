using BusinessService.Application.DTOs.Verification;
using BusinessService.Domain.Entities;

namespace BusinessService.Application.Interfaces;

public interface IBusinessVerificationService
{
    Task<BusinessVerificationDto> GetVerificationStatusAsync(Guid businessId);
    Task<BusinessVerificationDto> CreateVerificationAsync(Guid businessId);
    Task<BusinessVerificationDto> VerifyRequirementAsync(VerifyRequirementRequest request);
    Task SubmitIdVerificationAsync(SubmitIdVerificationRequest request);
    Task<VerificationStatusResponse> GetDetailedStatusAsync(Guid businessId);
    Task TriggerReverificationAsync(Guid businessId, string reason);
    Task CompleteReverificationAsync(Guid businessId);
    Task<List<BusinessVerificationDto>> GetBusinessesRequiringReverificationAsync();
}
