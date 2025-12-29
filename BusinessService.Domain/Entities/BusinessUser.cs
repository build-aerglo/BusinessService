namespace BusinessService.Domain.Entities;

/// <summary>
/// Multi-user access for businesses (Parent/Child system)
/// Parent: Full access (owner/manager)
/// Child: Limited read-only access (team members)
/// </summary>
public class BusinessUser
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? UserId { get; set; }

    // User details
    public string Email { get; set; } = default!;
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }

    // Role and permissions
    public BusinessUserRole Role { get; set; }
    public bool IsOwner { get; set; }

    // Account status
    public BusinessUserStatus Status { get; set; } = BusinessUserStatus.Pending;
    public DateTime? InvitedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? InvitationToken { get; set; }
    public DateTime? InvitationExpiresAt { get; set; }

    // Parent-managed settings
    public bool IsEnabled { get; set; } = true;
    public Guid? EnabledByUserId { get; set; }
    public DateTime? DisabledAt { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// Checks if user has specific permission
    /// </summary>
    public bool HasPermission(BusinessPermission permission)
    {
        if (!IsEnabled || Status != BusinessUserStatus.Active)
            return false;

        return Role switch
        {
            BusinessUserRole.Parent => true, // Parent has all permissions
            BusinessUserRole.Child => permission switch
            {
                BusinessPermission.ViewReviews => true,
                BusinessPermission.ViewAnalytics => true,
                _ => false
            },
            _ => false
        };
    }

    /// <summary>
    /// Enables the user account (parent only action)
    /// </summary>
    public void Enable(Guid enabledByUserId)
    {
        IsEnabled = true;
        EnabledByUserId = enabledByUserId;
        DisabledAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables the user account (parent only action)
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        DisabledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generates invitation token for new child user
    /// </summary>
    public void GenerateInvitation()
    {
        InvitationToken = Guid.NewGuid().ToString("N");
        InvitedAt = DateTime.UtcNow;
        InvitationExpiresAt = DateTime.UtcNow.AddDays(7);
        Status = BusinessUserStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Accepts the invitation and activates the account
    /// </summary>
    public bool AcceptInvitation(string token, Guid userId)
    {
        if (InvitationToken != token || DateTime.UtcNow > InvitationExpiresAt)
            return false;

        UserId = userId;
        Status = BusinessUserStatus.Active;
        AcceptedAt = DateTime.UtcNow;
        InvitationToken = null;
        InvitationExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }
}

/// <summary>
/// Business user roles
/// </summary>
public enum BusinessUserRole
{
    Parent = 0, // Full access (owner/manager)
    Child = 1   // Limited read-only access
}

/// <summary>
/// Business user account status
/// </summary>
public enum BusinessUserStatus
{
    Pending = 0,    // Invitation sent, not accepted
    Active = 1,     // Account active
    Disabled = 2,   // Disabled by parent
    Revoked = 3     // Access revoked
}

/// <summary>
/// Business permissions for access control
/// </summary>
public enum BusinessPermission
{
    ViewReviews,
    ViewAnalytics,
    ReplyToReviews,
    SubmitDisputes,
    ChangeSettings,
    ManageUsers,
    ManageSubscription
}
