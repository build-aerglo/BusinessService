namespace BusinessService.Domain.Entities;

/// <summary>
/// Extended business claim request for unclaimed business management (BS-008)
/// Tracks the full claim process with verification and approval workflow
/// </summary>
public class BusinessClaimRequest
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? ClaimantUserId { get; set; }

    // Claimant details
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Role { get; set; } = default!;

    // Verification documents
    public string? CacNumber { get; set; }
    public string? CacDocumentUrl { get; set; }
    public string? IdDocumentUrl { get; set; }
    public string? ProofOfOwnershipUrl { get; set; }
    public string? AdditionalDocumentsJson { get; set; }

    // Claim status
    public ClaimRequestStatus Status { get; set; } = ClaimRequestStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewNotes { get; set; }
    public string? RejectionReason { get; set; }

    // Verification checklist
    public bool CacVerified { get; set; }
    public bool IdVerified { get; set; }
    public bool OwnershipVerified { get; set; }
    public bool ContactVerified { get; set; }

    // Priority and escalation
    public ClaimPriority Priority { get; set; } = ClaimPriority.Normal;
    public bool IsEscalated { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public string? EscalationReason { get; set; }

    // Expected review time (24-48 hours default)
    public DateTime ExpectedReviewBy { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Sets expected review time (default 48 hours)
    /// </summary>
    public void SetExpectedReviewTime(int hours = 48)
    {
        ExpectedReviewBy = DateTime.UtcNow.AddHours(hours);
    }

    /// <summary>
    /// Approves the claim request
    /// </summary>
    public void Approve(Guid reviewerId, string? notes = null)
    {
        Status = ClaimRequestStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewerId;
        ReviewNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the claim request
    /// </summary>
    public void Reject(Guid reviewerId, string reason, string? notes = null)
    {
        Status = ClaimRequestStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewerId;
        RejectionReason = reason;
        ReviewNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Requests additional information
    /// </summary>
    public void RequestMoreInfo(Guid reviewerId, string notes)
    {
        Status = ClaimRequestStatus.MoreInfoRequired;
        ReviewedByUserId = reviewerId;
        ReviewNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Escalates the claim for priority review
    /// </summary>
    public void Escalate(string reason)
    {
        IsEscalated = true;
        EscalatedAt = DateTime.UtcNow;
        EscalationReason = reason;
        Priority = ClaimPriority.High;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if claim is overdue for review
    /// </summary>
    public bool IsOverdue => DateTime.UtcNow > ExpectedReviewBy && Status == ClaimRequestStatus.Pending;
}

/// <summary>
/// Claimant role types
/// </summary>
public enum ClaimantRole
{
    Owner = 0,
    Manager = 1,
    AuthorizedRepresentative = 2
}

/// <summary>
/// Claim request status
/// </summary>
public enum ClaimRequestStatus
{
    Pending = 0,
    UnderReview = 1,
    MoreInfoRequired = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5
}

/// <summary>
/// Claim priority levels
/// </summary>
public enum ClaimPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
