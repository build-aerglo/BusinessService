using BusinessService.Application.DTOs.Users;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;

namespace BusinessService.Application.Services;

public class BusinessUserService : IBusinessUserService
{
    private readonly IBusinessUserRepository _userRepository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IBusinessSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;

    public BusinessUserService(
        IBusinessUserRepository userRepository,
        IBusinessRepository businessRepository,
        IBusinessSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository)
    {
        _userRepository = userRepository;
        _businessRepository = businessRepository;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
    }

    public async Task<BusinessUsersListResponse> GetBusinessUsersAsync(Guid businessId)
    {
        var users = await _userRepository.FindByBusinessIdAsync(businessId);
        var activeCount = users.Count(u => u.Status == BusinessUserStatus.Active && u.IsEnabled);
        var pendingCount = users.Count(u => u.Status == BusinessUserStatus.Pending);

        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(businessId);
        var plan = subscription != null
            ? await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId)
            : await _planRepository.FindByTierAsync(SubscriptionTier.Basic);

        var maxUsers = plan?.UserLoginLimit ?? 1;

        return new BusinessUsersListResponse(
            businessId,
            users.Count,
            maxUsers,
            activeCount,
            pendingCount,
            users.Select(MapToDto).ToList()
        );
    }

    public async Task<BusinessUserDto?> GetBusinessUserByIdAsync(Guid userId)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<BusinessUserPermissionsDto> GetUserPermissionsAsync(Guid businessId, Guid userId)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null || user.BusinessId != businessId)
            throw new BusinessUserNotFoundException($"User with ID {userId} not found for business {businessId}");

        return new BusinessUserPermissionsDto(
            userId,
            businessId,
            user.Role,
            user.HasPermission(BusinessPermission.ViewReviews),
            user.HasPermission(BusinessPermission.ViewAnalytics),
            user.HasPermission(BusinessPermission.ReplyToReviews),
            user.HasPermission(BusinessPermission.SubmitDisputes),
            user.HasPermission(BusinessPermission.ChangeSettings),
            user.HasPermission(BusinessPermission.ManageUsers),
            user.HasPermission(BusinessPermission.ManageSubscription)
        );
    }

    public async Task<InvitationResponse> InviteUserAsync(InviteBusinessUserRequest request, Guid invitedByUserId)
    {
        var business = await _businessRepository.FindByIdAsync(request.BusinessId);
        if (business == null)
            throw new BusinessNotFoundException($"Business with ID {request.BusinessId} not found");

        if (await _userRepository.ExistsByEmailAndBusinessIdAsync(request.Email, request.BusinessId))
            throw new BusinessUserAlreadyExistsException($"User with email {request.Email} already exists for this business");

        var activeCount = await _userRepository.CountActiveByBusinessIdAsync(request.BusinessId);
        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(request.BusinessId);
        var plan = subscription != null
            ? await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId)
            : await _planRepository.FindByTierAsync(SubscriptionTier.Basic);

        var maxUsers = plan?.UserLoginLimit ?? 1;
        if (activeCount >= maxUsers)
            throw new UserLimitExceededException(maxUsers, activeCount);

        var user = new BusinessUser
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            Email = request.Email,
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Role = BusinessUserRole.Child,
            IsOwner = false,
            Status = BusinessUserStatus.Pending,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = invitedByUserId
        };

        user.GenerateInvitation();
        await _userRepository.AddAsync(user);

        var invitationUrl = $"https://aerglo.com/accept-invitation?token={user.InvitationToken}";

        return new InvitationResponse(
            user.Id,
            user.Email,
            user.InvitationToken!,
            user.InvitationExpiresAt!.Value,
            invitationUrl
        );
    }

    public async Task<BusinessUserDto> AcceptInvitationAsync(AcceptInvitationRequest request)
    {
        var user = await _userRepository.FindByInvitationTokenAsync(request.InvitationToken);
        if (user == null)
            throw new InvalidInvitationTokenException();

        if (DateTime.UtcNow > user.InvitationExpiresAt)
            throw new InvitationExpiredException();

        if (!user.AcceptInvitation(request.InvitationToken, request.UserId))
            throw new InvalidInvitationTokenException("Failed to accept invitation");

        await _userRepository.UpdateAsync(user);
        return MapToDto(user);
    }

    public async Task ResendInvitationAsync(Guid businessUserId, Guid requestedByUserId)
    {
        var user = await _userRepository.FindByIdAsync(businessUserId);
        if (user == null)
            throw new BusinessUserNotFoundException($"User with ID {businessUserId} not found");

        if (user.Status != BusinessUserStatus.Pending)
            throw new InvalidSubscriptionOperationException("Can only resend invitation for pending users");

        user.GenerateInvitation();
        await _userRepository.UpdateAsync(user);
    }

    public async Task<BusinessUserDto> EnableUserAsync(Guid businessUserId, Guid enabledByUserId)
    {
        var user = await _userRepository.FindByIdAsync(businessUserId);
        if (user == null)
            throw new BusinessUserNotFoundException($"User with ID {businessUserId} not found");

        user.Enable(enabledByUserId);
        await _userRepository.UpdateAsync(user);
        return MapToDto(user);
    }

    public async Task<BusinessUserDto> DisableUserAsync(Guid businessUserId, Guid disabledByUserId)
    {
        var user = await _userRepository.FindByIdAsync(businessUserId);
        if (user == null)
            throw new BusinessUserNotFoundException($"User with ID {businessUserId} not found");

        if (user.IsOwner)
            throw new InvalidSubscriptionOperationException("Cannot disable the owner account");

        user.Disable();
        await _userRepository.UpdateAsync(user);
        return MapToDto(user);
    }

    public async Task RemoveUserAsync(Guid businessUserId, Guid removedByUserId)
    {
        var user = await _userRepository.FindByIdAsync(businessUserId);
        if (user == null)
            throw new BusinessUserNotFoundException($"User with ID {businessUserId} not found");

        if (user.IsOwner)
            throw new InvalidSubscriptionOperationException("Cannot remove the owner account");

        user.Status = BusinessUserStatus.Revoked;
        user.IsEnabled = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
    }

    public async Task<bool> HasPermissionAsync(Guid businessId, Guid userId, string permission)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null || user.BusinessId != businessId)
            return false;

        var permissionEnum = Enum.TryParse<BusinessPermission>(permission, true, out var result)
            ? result
            : BusinessPermission.ViewReviews;

        return user.HasPermission(permissionEnum);
    }

    public async Task<bool> IsParentUserAsync(Guid businessId, Guid userId)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null || user.BusinessId != businessId)
            return false;

        return user.Role == BusinessUserRole.Parent;
    }

    private static BusinessUserDto MapToDto(BusinessUser user)
    {
        return new BusinessUserDto(
            user.Id,
            user.BusinessId,
            user.UserId,
            user.Email,
            user.Name,
            user.PhoneNumber,
            user.Role,
            user.Role.ToString(),
            user.IsOwner,
            user.Status,
            user.Status.ToString(),
            user.IsEnabled,
            user.InvitedAt,
            user.AcceptedAt,
            user.CreatedAt,
            user.UpdatedAt
        );
    }
}
