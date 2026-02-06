namespace BusinessService.Domain.Entities;

/// <summary>
/// Business verification tracking for the badge system.
/// Levels: Standard (ribbon), Verified (check), Trusted (shield)
/// </summary>
public class BusinessVerification
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }

    // Standard Level Requirements
    public bool CacVerified { get; set; }
    public string? CacNumber { get; set; }
    public DateTime? CacVerifiedAt { get; set; }

    public bool PhoneVerified { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? PhoneVerifiedAt { get; set; }

    public bool EmailVerified { get; set; }
    public string? Email { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }

    public bool AddressVerified { get; set; }
    public string? AddressProofUrl { get; set; }
    public DateTime? AddressVerifiedAt { get; set; }

    // ID Verification (CAC, TIN, etc.)
    public bool IdVerified { get; set; }
    public string? IdVerificationStatus { get; set; }

    // Verification Progress (percentage)
    public decimal VerificationProgress { get; set; }

    // Verified Level Requirements (additional)
    public bool OnlinePresenceVerified { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? SocialMediaUrl { get; set; }
    public DateTime? OnlinePresenceVerifiedAt { get; set; }

    public bool OtherIdsVerified { get; set; }
    public string? TinNumber { get; set; }
    public string? LicenseNumber { get; set; }
    public string? OtherIdDocumentUrl { get; set; }
    public DateTime? OtherIdsVerifiedAt { get; set; }

    // Trusted Level Requirements (additional)
    public bool BusinessDomainEmailVerified { get; set; }
    public string? BusinessDomainEmail { get; set; }
    public DateTime? BusinessDomainEmailVerifiedAt { get; set; }

    // Computed verification level
    public VerificationLevel Level => CalculateVerificationLevel();

    // Re-verification tracking
    public bool RequiresReverification { get; set; }
    public string? ReverificationReason { get; set; }
    public DateTime? ReverificationRequestedAt { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? VerifiedByUserId { get; set; }

    /// <summary>
    /// Calculates the verification level based on completed requirements
    /// </summary>
    public VerificationLevel CalculateVerificationLevel()
    {
        // Check Trusted level (all requirements)
        if (HasStandardRequirements() && HasVerifiedRequirements() && HasTrustedRequirements())
            return VerificationLevel.Trusted;

        // Check Verified level
        if (HasStandardRequirements() && HasVerifiedRequirements())
            return VerificationLevel.Verified;

        // Check Standard level
        if (HasStandardRequirements())
            return VerificationLevel.Standard;

        return VerificationLevel.Unverified;
    }

    private bool HasStandardRequirements()
    {
        return CacVerified && (PhoneVerified || EmailVerified) && AddressVerified;
    }

    private bool HasVerifiedRequirements()
    {
        return OnlinePresenceVerified && OtherIdsVerified;
    }

    private bool HasTrustedRequirements()
    {
        return BusinessDomainEmailVerified;
    }

    /// <summary>
    /// Triggers re-verification when critical info changes
    /// </summary>
    public void TriggerReverification(string reason)
    {
        RequiresReverification = true;
        ReverificationReason = reason;
        ReverificationRequestedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes re-verification process
    /// </summary>
    public void CompleteReverification()
    {
        RequiresReverification = false;
        ReverificationReason = null;
        ReverificationRequestedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Verification levels for businesses
/// </summary>
public enum VerificationLevel
{
    Unverified = 0,
    Standard = 1,   // Ribbon icon
    Verified = 2,   // Check icon
    Trusted = 3     // Shield icon
}
