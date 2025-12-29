using BusinessService.Application.DTOs.Users;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessUserController : ControllerBase
{
    private readonly IBusinessUserService _userService;
    private readonly ILogger<BusinessUserController> _logger;

    public BusinessUserController(
        IBusinessUserService userService,
        ILogger<BusinessUserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users for a business
    /// </summary>
    [HttpGet("business/{businessId:guid}")]
    public async Task<ActionResult<BusinessUsersListResponse>> GetBusinessUsers(Guid businessId)
    {
        try
        {
            var result = await _userService.GetBusinessUsersAsync(businessId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred while fetching users" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<BusinessUserDto>> GetUserById(Guid userId)
    {
        try
        {
            var user = await _userService.GetBusinessUserByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while fetching the user" });
        }
    }

    /// <summary>
    /// Get user permissions
    /// </summary>
    [HttpGet("business/{businessId:guid}/user/{userId:guid}/permissions")]
    public async Task<ActionResult<BusinessUserPermissionsDto>> GetUserPermissions(Guid businessId, Guid userId)
    {
        try
        {
            var permissions = await _userService.GetUserPermissionsAsync(businessId, userId);
            return Ok(permissions);
        }
        catch (BusinessUserNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Invite a new user
    /// </summary>
    [HttpPost("invite")]
    public async Task<ActionResult<InvitationResponse>> InviteUser(
        [FromBody] InviteBusinessUserRequest request,
        [FromHeader(Name = "X-User-Id")] Guid invitedByUserId)
    {
        try
        {
            var result = await _userService.InviteUserAsync(request, invitedByUserId);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessUserAlreadyExistsException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UserLimitExceededException ex)
        {
            return BadRequest(new { message = ex.Message, maxUsers = ex.MaxUsers, currentUsers = ex.CurrentUsers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting user to business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred while inviting user" });
        }
    }

    /// <summary>
    /// Accept an invitation
    /// </summary>
    [HttpPost("accept-invitation")]
    public async Task<ActionResult<BusinessUserDto>> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        try
        {
            var user = await _userService.AcceptInvitationAsync(request);
            return Ok(user);
        }
        catch (InvalidInvitationTokenException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvitationExpiredException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation");
            return StatusCode(500, new { message = "An error occurred while accepting invitation" });
        }
    }

    /// <summary>
    /// Resend invitation
    /// </summary>
    [HttpPost("{businessUserId:guid}/resend-invitation")]
    public async Task<IActionResult> ResendInvitation(
        Guid businessUserId,
        [FromHeader(Name = "X-User-Id")] Guid requestedByUserId)
    {
        try
        {
            await _userService.ResendInvitationAsync(businessUserId, requestedByUserId);
            return Ok(new { message = "Invitation resent successfully" });
        }
        catch (BusinessUserNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidSubscriptionOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending invitation for user {UserId}", businessUserId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Enable a user
    /// </summary>
    [HttpPut("{businessUserId:guid}/enable")]
    public async Task<ActionResult<BusinessUserDto>> EnableUser(
        Guid businessUserId,
        [FromHeader(Name = "X-User-Id")] Guid enabledByUserId)
    {
        try
        {
            var user = await _userService.EnableUserAsync(businessUserId, enabledByUserId);
            return Ok(user);
        }
        catch (BusinessUserNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling user {UserId}", businessUserId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Disable a user
    /// </summary>
    [HttpPut("{businessUserId:guid}/disable")]
    public async Task<ActionResult<BusinessUserDto>> DisableUser(
        Guid businessUserId,
        [FromHeader(Name = "X-User-Id")] Guid disabledByUserId)
    {
        try
        {
            var user = await _userService.DisableUserAsync(businessUserId, disabledByUserId);
            return Ok(user);
        }
        catch (BusinessUserNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidSubscriptionOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling user {UserId}", businessUserId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Remove a user
    /// </summary>
    [HttpDelete("{businessUserId:guid}")]
    public async Task<IActionResult> RemoveUser(
        Guid businessUserId,
        [FromHeader(Name = "X-User-Id")] Guid removedByUserId)
    {
        try
        {
            await _userService.RemoveUserAsync(businessUserId, removedByUserId);
            return Ok(new { message = "User removed successfully" });
        }
        catch (BusinessUserNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidSubscriptionOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId}", businessUserId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
