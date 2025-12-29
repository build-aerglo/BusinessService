using BusinessService.Domain.Entities;

namespace BusinessService.Application.DTOs.Users;

/// <summary>
/// Business user details DTO
/// </summary>
public record BusinessUserDto(
    Guid Id,
    Guid BusinessId,
    Guid? UserId,
    string Email,
    string? Name,
    string? PhoneNumber,
    BusinessUserRole Role,
    string RoleName,
    bool IsOwner,
    BusinessUserStatus Status,
    string StatusName,
    bool IsEnabled,
    DateTime? InvitedAt,
    DateTime? AcceptedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Business user permissions DTO
/// </summary>
public record BusinessUserPermissionsDto(
    Guid UserId,
    Guid BusinessId,
    BusinessUserRole Role,
    bool CanViewReviews,
    bool CanViewAnalytics,
    bool CanReplyToReviews,
    bool CanSubmitDisputes,
    bool CanChangeSettings,
    bool CanManageUsers,
    bool CanManageSubscription
);

/// <summary>
/// Request to invite a new user (child account)
/// </summary>
public class InviteBusinessUserRequest
{
    public Guid BusinessId { get; set; }
    public string Email { get; set; } = default!;
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Request to accept an invitation
/// </summary>
public class AcceptInvitationRequest
{
    public string InvitationToken { get; set; } = default!;
    public Guid UserId { get; set; }
    public string? Password { get; set; }
}

/// <summary>
/// Request to update user status
/// </summary>
public class UpdateBusinessUserStatusRequest
{
    public Guid BusinessUserId { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Business users list response
/// </summary>
public record BusinessUsersListResponse(
    Guid BusinessId,
    int TotalUsers,
    int MaxUsers,
    int ActiveUsers,
    int PendingInvitations,
    List<BusinessUserDto> Users
);

/// <summary>
/// Invitation response
/// </summary>
public record InvitationResponse(
    Guid BusinessUserId,
    string Email,
    string InvitationToken,
    DateTime ExpiresAt,
    string InvitationUrl
);

/// <summary>
/// Password reset request (child accounts require parent authorization)
/// </summary>
public class ChildPasswordResetRequest
{
    public Guid BusinessUserId { get; set; }
    public Guid AuthorizingParentId { get; set; }
}
