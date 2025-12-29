using BusinessService.Application.DTOs.Claims;
using BusinessService.Domain.Entities;

namespace BusinessService.Application.Interfaces;

public interface IBusinessClaimService
{
    // Claim submission
    Task<BusinessClaimRequestDto> SubmitClaimAsync(SubmitBusinessClaimRequest request, Guid? userId = null);
    Task<ClaimStatusResponse> GetClaimStatusAsync(Guid claimId);
    Task<BusinessClaimRequestDto?> GetClaimByIdAsync(Guid claimId);
    Task<List<BusinessClaimRequestDto>> GetClaimsByBusinessIdAsync(Guid businessId);

    // Admin review
    Task<PendingClaimsListResponse> GetPendingClaimsAsync();
    Task<BusinessClaimRequestDto> ReviewClaimAsync(ReviewClaimRequest request);
    Task<BusinessClaimRequestDto> EscalateClaimAsync(Guid claimId, string reason);

    // Unclaimed business discovery
    Task<List<UnclaimedBusinessDto>> GetUnclaimedBusinessesAsync(int limit = 50, int offset = 0);
    Task<bool> IsBusinessClaimedAsync(Guid businessId);
    Task<bool> HasPendingClaimAsync(Guid businessId);
}
