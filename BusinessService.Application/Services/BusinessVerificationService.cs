using BusinessService.Application.DTOs.Verification;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;

namespace BusinessService.Application.Services;

public class BusinessVerificationService : IBusinessVerificationService
{
    private readonly IBusinessVerificationRepository _verificationRepository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IIdVerificationRequestRepository _idVerificationRequestRepository;

    public BusinessVerificationService(
        IBusinessVerificationRepository verificationRepository,
        IBusinessRepository businessRepository,
        IIdVerificationRequestRepository idVerificationRequestRepository)
    {
        _verificationRepository = verificationRepository;
        _businessRepository = businessRepository;
        _idVerificationRequestRepository = idVerificationRequestRepository;
    }

    public async Task<BusinessVerificationDto> GetVerificationStatusAsync(Guid businessId)
    {
        var verification = await _verificationRepository.FindByBusinessIdAsync(businessId);
        if (verification == null)
            throw new VerificationNotFoundException($"Verification record not found for business {businessId}");

        return MapToDto(verification);
    }

    public async Task<BusinessVerificationDto> CreateVerificationAsync(Guid businessId)
    {
        var business = await _businessRepository.FindByIdAsync(businessId);
        if (business == null)
            throw new BusinessNotFoundException($"Business with ID {businessId} not found");

        if (await _verificationRepository.ExistsByBusinessIdAsync(businessId))
            throw new VerificationRequiredException("Verification record already exists", "Duplicate");

        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _verificationRepository.AddAsync(verification);
        return MapToDto(verification);
    }

    public async Task<BusinessVerificationDto> VerifyRequirementAsync(VerifyRequirementRequest request)
    {
        var verification = await _verificationRepository.FindByBusinessIdAsync(request.BusinessId);
        if (verification == null)
        {
            verification = new BusinessVerification
            {
                Id = Guid.NewGuid(),
                BusinessId = request.BusinessId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _verificationRepository.AddAsync(verification);
        }

        switch (request.RequirementType)
        {
            case VerificationRequirementType.Cac:
                verification.CacVerified = true;
                verification.CacNumber = request.Value;
                verification.CacVerifiedAt = DateTime.UtcNow;
                break;

            case VerificationRequirementType.Phone:
                verification.PhoneVerified = true;
                verification.PhoneNumber = request.Value;
                verification.PhoneVerifiedAt = DateTime.UtcNow;
                break;

            case VerificationRequirementType.Email:
                verification.EmailVerified = true;
                verification.Email = request.Value;
                verification.EmailVerifiedAt = DateTime.UtcNow;
                break;

            case VerificationRequirementType.Address:
                verification.AddressVerified = true;
                verification.AddressProofUrl = request.DocumentUrl;
                verification.AddressVerifiedAt = DateTime.UtcNow;
                break;

            case VerificationRequirementType.OnlinePresence:
                verification.OnlinePresenceVerified = true;
                verification.WebsiteUrl = request.Value;
                verification.OnlinePresenceVerifiedAt = DateTime.UtcNow;
                break;

            case VerificationRequirementType.OtherIds:
                verification.OtherIdsVerified = true;
                verification.OtherIdDocumentUrl = request.DocumentUrl;
                verification.OtherIdsVerifiedAt = DateTime.UtcNow;
                break;

            case VerificationRequirementType.BusinessDomainEmail:
                verification.BusinessDomainEmailVerified = true;
                verification.BusinessDomainEmail = request.Value;
                verification.BusinessDomainEmailVerifiedAt = DateTime.UtcNow;
                break;
        }

        verification.UpdatedAt = DateTime.UtcNow;
        await _verificationRepository.UpdateAsync(verification);

        return MapToDto(verification);
    }

    public async Task SubmitIdVerificationAsync(SubmitIdVerificationRequest request)
    {
        var business = await _businessRepository.FindByIdAsync(request.BusinessId);
        if (business == null)
            throw new BusinessNotFoundException($"Business with ID {request.BusinessId} not found");

        // Insert into id_verification_request table
        var idVerificationRequest = new IdVerificationRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            IdVerificationType = request.IdVerificationType,
            IdVerificationNumber = request.IdVerificationNumber,
            IdVerificationUrl = request.IdVerificationUrl,
            IdVerificationName = request.IdVerificationName,
            CreatedAt = DateTime.UtcNow
        };

        await _idVerificationRequestRepository.AddAsync(idVerificationRequest);

        // Update business_verification: id_verified = false, id_verification_status = 'pending'
        await _verificationRepository.UpdateIdVerificationStatusAsync(request.BusinessId, false, "pending");
    }

    public async Task<VerificationStatusResponse> GetDetailedStatusAsync(Guid businessId)
    {
        var verification = await _verificationRepository.FindByBusinessIdAsync(businessId);

        var requirements = new List<VerificationRequirementStatus>
        {
            new(VerificationRequirementType.Cac, "CAC Registration", "Corporate Affairs Commission registration number",
                verification?.CacVerified ?? false, verification?.CacVerifiedAt, true, VerificationLevel.Standard),
            new(VerificationRequirementType.Phone, "Phone Verification", "Verified phone number via OTP",
                verification?.PhoneVerified ?? false, verification?.PhoneVerifiedAt, true, VerificationLevel.Standard),
            new(VerificationRequirementType.Email, "Email Verification", "Verified email address",
                verification?.EmailVerified ?? false, verification?.EmailVerifiedAt, true, VerificationLevel.Standard),
            new(VerificationRequirementType.Address, "Address Verification", "Verified business address with utility bill or site visit",
                verification?.AddressVerified ?? false, verification?.AddressVerifiedAt, true, VerificationLevel.Standard),
            new(VerificationRequirementType.OnlinePresence, "Online Presence", "Website or active social media profile",
                verification?.OnlinePresenceVerified ?? false, verification?.OnlinePresenceVerifiedAt, true, VerificationLevel.Verified),
            new(VerificationRequirementType.OtherIds, "Additional IDs", "TIN, licenses, or other business documents",
                verification?.OtherIdsVerified ?? false, verification?.OtherIdsVerifiedAt, true, VerificationLevel.Verified),
            new(VerificationRequirementType.BusinessDomainEmail, "Business Domain Email", "Email address from company domain",
                verification?.BusinessDomainEmailVerified ?? false, verification?.BusinessDomainEmailVerifiedAt, true, VerificationLevel.Trusted)
        };

        var currentLevel = verification?.Level ?? VerificationLevel.Unverified;
        var nextLevel = currentLevel switch
        {
            VerificationLevel.Unverified => VerificationLevel.Standard,
            VerificationLevel.Standard => VerificationLevel.Verified,
            VerificationLevel.Verified => VerificationLevel.Trusted,
            _ => VerificationLevel.Trusted
        };

        var canUpgrade = currentLevel != VerificationLevel.Trusted;

        return new VerificationStatusResponse(currentLevel, nextLevel, requirements, canUpgrade);
    }

    public async Task TriggerReverificationAsync(Guid businessId, string reason)
    {
        var verification = await _verificationRepository.FindByBusinessIdAsync(businessId);
        if (verification == null)
            throw new VerificationNotFoundException($"Verification record not found for business {businessId}");

        verification.TriggerReverification(reason);
        await _verificationRepository.UpdateAsync(verification);
    }

    public async Task CompleteReverificationAsync(Guid businessId)
    {
        var verification = await _verificationRepository.FindByBusinessIdAsync(businessId);
        if (verification == null)
            throw new VerificationNotFoundException($"Verification record not found for business {businessId}");

        verification.CompleteReverification();
        await _verificationRepository.UpdateAsync(verification);
    }

    public async Task<List<BusinessVerificationDto>> GetBusinessesRequiringReverificationAsync()
    {
        var verifications = await _verificationRepository.FindRequiringReverificationAsync();
        return verifications.Select(MapToDto).ToList();
    }

    private static BusinessVerificationDto MapToDto(BusinessVerification v)
    {
        var completedRequirements = 0;
        if (v.CacVerified) completedRequirements++;
        if (v.PhoneVerified) completedRequirements++;
        if (v.EmailVerified) completedRequirements++;
        if (v.AddressVerified) completedRequirements++;
        if (v.OnlinePresenceVerified) completedRequirements++;
        if (v.OtherIdsVerified) completedRequirements++;
        if (v.BusinessDomainEmailVerified) completedRequirements++;

        const int totalRequirements = 7;
        var progress = Math.Round((decimal)completedRequirements / totalRequirements * 100, 2);

        var levelIcon = v.Level switch
        {
            VerificationLevel.Standard => "ribbon",
            VerificationLevel.Verified => "check",
            VerificationLevel.Trusted => "shield",
            _ => "none"
        };

        return new BusinessVerificationDto(
            v.Id,
            v.BusinessId,
            v.Level,
            v.Level.ToString(),
            levelIcon,
            v.CacVerified,
            v.PhoneVerified,
            v.EmailVerified,
            v.AddressVerified,
            v.IdVerified,
            v.IdVerificationStatus,
            v.OnlinePresenceVerified,
            v.OtherIdsVerified,
            v.BusinessDomainEmailVerified,
            v.RequiresReverification,
            v.ReverificationReason,
            completedRequirements,
            totalRequirements,
            progress,
            v.CreatedAt,
            v.UpdatedAt
        );
    }
}
