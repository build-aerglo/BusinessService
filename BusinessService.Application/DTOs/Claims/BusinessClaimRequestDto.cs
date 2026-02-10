using BusinessService.Domain.Entities;

namespace BusinessService.Application.DTOs.Claims;

/// <summary>
/// Business claim request DTO
/// </summary>
public record BusinessClaimRequestDto(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    Guid? ClaimantUserId,
    string FullName,
    string Email,
    string PhoneNumber,
    string Role,
    string RoleName,
    ClaimRequestStatus Status,
    string StatusName,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? ReviewNotes,
    string? RejectionReason,
    bool IsOverdue,
    DateTime ExpectedReviewBy,
    ClaimVerificationStatusDto VerificationStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Claim verification status
/// </summary>
public record ClaimVerificationStatusDto(
    bool CacVerified,
    bool IdVerified,
    bool OwnershipVerified,
    bool ContactVerified,
    int CompletedChecks,
    int TotalChecks
);

/// <summary>
/// Request to submit a business claim
/// </summary>
public class SubmitBusinessClaimRequest
{
    public Guid BusinessId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Role { get; set; } = default!;
    public Guid? CategoryId { get; set; }
    public string? CacNumber { get; set; }
    public string? CacDocumentUrl { get; set; }
    public string? IdDocumentUrl { get; set; }
    public string? ProofOfOwnershipUrl { get; set; }
}

/// <summary>
/// Request to review a claim
/// </summary>
public class ReviewClaimRequest
{
    public Guid ClaimId { get; set; }
    public Guid ReviewerId { get; set; }
    public ClaimReviewAction Action { get; set; }
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Claim review actions
/// </summary>
public enum ClaimReviewAction
{
    Approve,
    Reject,
    RequestMoreInfo
}

/// <summary>
/// Business claim status response
/// </summary>
public record ClaimStatusResponse(
    Guid ClaimId,
    ClaimRequestStatus Status,
    string StatusMessage,
    DateTime? ExpectedCompletionDate,
    string? NextSteps
);

/// <summary>
/// Pending claims list for admin
/// </summary>
public record PendingClaimsListResponse(
    int TotalPending,
    int Overdue,
    int HighPriority,
    List<BusinessClaimRequestDto> Claims
);

/// <summary>
/// Unclaimed business info
/// </summary>
public record UnclaimedBusinessDto(
    Guid BusinessId,
    string BusinessName,
    string? BusinessAddress,
    string? Category,
    decimal AverageRating,
    int ReviewCount,
    DateTime CreatedAt,
    bool HasPendingClaim
);
