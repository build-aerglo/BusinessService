using BusinessService.Application.DTOs.Users;

namespace BusinessService.Application.Interfaces;

public interface IBusinessUserService
{
    // User management
    Task<BusinessUsersListResponse> GetBusinessUsersAsync(Guid businessId);
    Task<BusinessUserDto?> GetBusinessUserByIdAsync(Guid userId);
    Task<BusinessUserPermissionsDto> GetUserPermissionsAsync(Guid businessId, Guid userId);

    // Invitation management
    Task<InvitationResponse> InviteUserAsync(InviteBusinessUserRequest request, Guid invitedByUserId);
    Task<BusinessUserDto> AcceptInvitationAsync(AcceptInvitationRequest request);
    Task ResendInvitationAsync(Guid businessUserId, Guid requestedByUserId);

    // User status management
    Task<BusinessUserDto> EnableUserAsync(Guid businessUserId, Guid enabledByUserId);
    Task<BusinessUserDto> DisableUserAsync(Guid businessUserId, Guid disabledByUserId);
    Task RemoveUserAsync(Guid businessUserId, Guid removedByUserId);

    // Permission checks
    Task<bool> HasPermissionAsync(Guid businessId, Guid userId, string permission);
    Task<bool> IsParentUserAsync(Guid businessId, Guid userId);
}
