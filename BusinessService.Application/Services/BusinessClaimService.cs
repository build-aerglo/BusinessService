using BusinessService.Application.DTOs.Claims;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;

namespace BusinessService.Application.Services;

public class BusinessClaimService : IBusinessClaimService
{
    private readonly IBusinessClaimRequestRepository _claimRepository;
    private readonly IBusinessRepository _businessRepository;

    public BusinessClaimService(
        IBusinessClaimRequestRepository claimRepository,
        IBusinessRepository businessRepository)
    {
        _claimRepository = claimRepository;
        _businessRepository = businessRepository;
    }

    public async Task<BusinessClaimRequestDto> SubmitClaimAsync(SubmitBusinessClaimRequest request, Guid? userId = null)
    {
        var business = await _businessRepository.FindByIdAsync(request.BusinessId);
        if (business == null)
            throw new BusinessNotFoundException($"Business with ID {request.BusinessId} not found");

        if (business.IsVerified)
            throw new BusinessAlreadyClaimedException("This business has already been claimed and verified");

        if (await _claimRepository.ExistsPendingByBusinessIdAsync(request.BusinessId))
            throw new ClaimAlreadyExistsException();

        var claim = new BusinessClaimRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            ClaimantUserId = userId,
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = request.Role,
            CacNumber = request.CacNumber,
            CacDocumentUrl = request.CacDocumentUrl,
            IdDocumentUrl = request.IdDocumentUrl,
            ProofOfOwnershipUrl = request.ProofOfOwnershipUrl,
            BusinessCategory = request.CategoryId,
            Status = ClaimRequestStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            Priority = ClaimPriority.Normal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        claim.SetExpectedReviewTime(48);
        await _claimRepository.AddAsync(claim);

        return MapToDto(claim, business.Name);
    }

    public async Task<ClaimStatusResponse> GetClaimStatusAsync(Guid claimId)
    {
        var claim = await _claimRepository.FindByIdAsync(claimId);
        if (claim == null)
            throw new ClaimNotFoundException($"Claim with ID {claimId} not found");

        var statusMessage = claim.Status switch
        {
            ClaimRequestStatus.Pending => "Your claim is pending review",
            ClaimRequestStatus.UnderReview => "Your claim is currently being reviewed",
            ClaimRequestStatus.MoreInfoRequired => "Additional information is required",
            ClaimRequestStatus.Approved => "Your claim has been approved",
            ClaimRequestStatus.Rejected => $"Your claim was rejected: {claim.RejectionReason}",
            ClaimRequestStatus.Cancelled => "Your claim has been cancelled",
            _ => "Unknown status"
        };

        var nextSteps = claim.Status switch
        {
            ClaimRequestStatus.Pending => "Please wait for our team to review your submission",
            ClaimRequestStatus.UnderReview => "Our team is verifying your documents",
            ClaimRequestStatus.MoreInfoRequired => claim.ReviewNotes ?? "Please provide additional information",
            ClaimRequestStatus.Approved => "You can now manage your business on Aerglo",
            ClaimRequestStatus.Rejected => "You may submit a new claim with updated information",
            _ => null
        };

        return new ClaimStatusResponse(
            claimId,
            claim.Status,
            statusMessage,
            claim.Status == ClaimRequestStatus.Pending ? claim.ExpectedReviewBy : null,
            nextSteps
        );
    }

    public async Task<BusinessClaimRequestDto?> GetClaimByIdAsync(Guid claimId)
    {
        var claim = await _claimRepository.FindByIdAsync(claimId);
        if (claim == null) return null;

        var business = await _businessRepository.FindByIdAsync(claim.BusinessId);
        return MapToDto(claim, business?.Name ?? "Unknown");
    }

    public async Task<List<BusinessClaimRequestDto>> GetClaimsByBusinessIdAsync(Guid businessId)
    {
        var claims = await _claimRepository.FindByBusinessIdAsync(businessId);
        var business = await _businessRepository.FindByIdAsync(businessId);
        return claims.Select(c => MapToDto(c, business?.Name ?? "Unknown")).ToList();
    }

    public async Task<PendingClaimsListResponse> GetPendingClaimsAsync()
    {
        var claims = await _claimRepository.FindPendingAsync();
        var overdueCount = await _claimRepository.CountOverdueAsync();
        var highPriorityCount = claims.Count(c => c.Priority >= ClaimPriority.High);

        var claimDtos = new List<BusinessClaimRequestDto>();
        foreach (var claim in claims)
        {
            var business = await _businessRepository.FindByIdAsync(claim.BusinessId);
            claimDtos.Add(MapToDto(claim, business?.Name ?? "Unknown"));
        }

        return new PendingClaimsListResponse(
            claims.Count,
            overdueCount,
            highPriorityCount,
            claimDtos
        );
    }

    public async Task<BusinessClaimRequestDto> ReviewClaimAsync(ReviewClaimRequest request)
    {
        var claim = await _claimRepository.FindByIdAsync(request.ClaimId);
        if (claim == null)
            throw new ClaimNotFoundException($"Claim with ID {request.ClaimId} not found");

        switch (request.Action)
        {
            case ClaimReviewAction.Approve:
                claim.Approve(request.ReviewerId, request.Notes);
                var business = await _businessRepository.FindByIdAsync(claim.BusinessId);
                if (business != null)
                {
                    business.IsVerified = true;
                    business.BusinessStatus = "approved";
                    business.UpdatedAt = DateTime.UtcNow;
                    await _businessRepository.UpdateProfileAsync(business);
                }
                break;

            case ClaimReviewAction.Reject:
                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                    throw new InvalidClaimOperationException("Rejection reason is required");
                claim.Reject(request.ReviewerId, request.RejectionReason, request.Notes);
                break;

            case ClaimReviewAction.RequestMoreInfo:
                if (string.IsNullOrWhiteSpace(request.Notes))
                    throw new InvalidClaimOperationException("Notes are required when requesting more info");
                claim.RequestMoreInfo(request.ReviewerId, request.Notes);
                break;
        }

        await _claimRepository.UpdateAsync(claim);

        var businessForDto = await _businessRepository.FindByIdAsync(claim.BusinessId);
        return MapToDto(claim, businessForDto?.Name ?? "Unknown");
    }

    public async Task<BusinessClaimRequestDto> EscalateClaimAsync(Guid claimId, string reason)
    {
        var claim = await _claimRepository.FindByIdAsync(claimId);
        if (claim == null)
            throw new ClaimNotFoundException($"Claim with ID {claimId} not found");

        claim.Escalate(reason);
        await _claimRepository.UpdateAsync(claim);

        var business = await _businessRepository.FindByIdAsync(claim.BusinessId);
        return MapToDto(claim, business?.Name ?? "Unknown");
    }

    public async Task<List<UnclaimedBusinessDto>> GetUnclaimedBusinessesAsync(int limit = 50, int offset = 0)
    {
        // This would typically be a specialized query in the repository
        // For now, we'll return an empty list as a placeholder
        return new List<UnclaimedBusinessDto>();
    }

    public async Task<bool> IsBusinessClaimedAsync(Guid businessId)
    {
        var business = await _businessRepository.FindByIdAsync(businessId);
        return business?.IsVerified ?? false;
    }

    public async Task<bool> HasPendingClaimAsync(Guid businessId)
    {
        return await _claimRepository.ExistsPendingByBusinessIdAsync(businessId);
    }

    private static BusinessClaimRequestDto MapToDto(BusinessClaimRequest claim, string businessName)
    {
        var verificationStatus = new ClaimVerificationStatusDto(
            claim.CacVerified,
            claim.IdVerified,
            claim.OwnershipVerified,
            claim.ContactVerified,
            (claim.CacVerified ? 1 : 0) + (claim.IdVerified ? 1 : 0) +
            (claim.OwnershipVerified ? 1 : 0) + (claim.ContactVerified ? 1 : 0),
            4
        );

        return new BusinessClaimRequestDto(
            claim.Id,
            claim.BusinessId,
            businessName,
            claim.ClaimantUserId,
            claim.FullName,
            claim.Email,
            claim.PhoneNumber,
            claim.Role,
            claim.Role,
            claim.Status,
            claim.Status.ToString(),
            claim.SubmittedAt,
            claim.ReviewedAt,
            claim.ReviewNotes,
            claim.RejectionReason,
            claim.IsOverdue,
            claim.ExpectedReviewBy,
            verificationStatus,
            claim.CreatedAt,
            claim.UpdatedAt
        );
    }
}
