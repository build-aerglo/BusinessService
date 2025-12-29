using BusinessService.Domain.Entities;

namespace BusinessService.Application.DTOs.Verification;

/// <summary>
/// Business verification status DTO
/// </summary>
public record BusinessVerificationDto(
    Guid Id,
    Guid BusinessId,
    VerificationLevel Level,
    string LevelName,
    string LevelIcon,

    // Standard requirements
    bool CacVerified,
    bool PhoneVerified,
    bool EmailVerified,
    bool AddressVerified,

    // Verified requirements
    bool OnlinePresenceVerified,
    bool OtherIdsVerified,

    // Trusted requirements
    bool BusinessDomainEmailVerified,

    // Re-verification
    bool RequiresReverification,
    string? ReverificationReason,

    // Progress
    int CompletedRequirements,
    int TotalRequirements,
    decimal VerificationProgress,

    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Request to verify a specific requirement
/// </summary>
public class VerifyRequirementRequest
{
    public Guid BusinessId { get; set; }
    public VerificationRequirementType RequirementType { get; set; }
    public string? Value { get; set; }
    public string? DocumentUrl { get; set; }
}

/// <summary>
/// Verification requirement types
/// </summary>
public enum VerificationRequirementType
{
    Cac,
    Phone,
    Email,
    Address,
    OnlinePresence,
    OtherIds,
    BusinessDomainEmail
}

/// <summary>
/// Response for verification status
/// </summary>
public record VerificationStatusResponse(
    VerificationLevel CurrentLevel,
    VerificationLevel NextLevel,
    List<VerificationRequirementStatus> Requirements,
    bool CanUpgrade
);

/// <summary>
/// Status of individual verification requirement
/// </summary>
public record VerificationRequirementStatus(
    VerificationRequirementType Type,
    string Name,
    string Description,
    bool IsCompleted,
    DateTime? CompletedAt,
    bool IsRequired,
    VerificationLevel RequiredForLevel
);
