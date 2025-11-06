using BusinessService.Application.DTOs;
using BusinessService.Application.DTOs.Settings;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api")]
public class BusinessSettingsController : ControllerBase
{
    private readonly IBusinessSettingsService _service;
    private readonly ILogger<BusinessSettingsController> _logger;

    public BusinessSettingsController(
        IBusinessSettingsService service,
        ILogger<BusinessSettingsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ========== Business Settings (Parent Rep Only) ==========

    /// <summary>
    /// Gets business settings (DnD, ReviewsPrivate).
    /// Creates default if none exist.
    /// </summary>
    [HttpGet("business/{businessId:guid}/settings")]
    public async Task<IActionResult> GetBusinessSettings(Guid businessId)
    {
        try
        {
            var result = await _service.GetBusinessSettingsAsync(businessId);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            _logger.LogWarning(ex, "Business not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates business settings (DnD, ReviewsPrivate).
    /// Only parent business rep can modify.
    /// </summary>
    [HttpPatch("business/{businessId:guid}/settings")]
    public async Task<IActionResult> UpdateBusinessSettings(
        Guid businessId,
        [FromBody] UpdateBusinessSettingsRequest request,
        [FromQuery] Guid? currentUserId)
    {
        try
        {
            var userId = currentUserId ?? Guid.Empty;
            if (userId == Guid.Empty)
                return BadRequest(new { error = "Current user ID is required" });

            var result = await _service.UpdateBusinessSettingsAsync(businessId, request, userId);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            _logger.LogWarning(ex, "Business not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedSettingsAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access: {Message}", ex.Message);
            return StatusCode(403, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Extends DnD mode duration (support users only).
    /// </summary>
    [HttpPost("business/{businessId:guid}/settings/dnd-mode/extend")]
    public async Task<IActionResult> ExtendDndMode(
        Guid businessId,
        [FromQuery] int additionalHours,
        [FromQuery] Guid? currentUserId)
    {
        try
        {
            
            var userId = currentUserId ?? Guid.Empty;
            if (userId == Guid.Empty)
                return BadRequest(new { error = "Current user ID is required" });

            if (additionalHours <= 0 || additionalHours > 168)
                return BadRequest(new { error = "Additional hours must be between 1 and 168" });

            var result = await _service.ExtendDndModeAsync(businessId, additionalHours, userId);
            return Ok(result);
        }
        catch (BusinessSettingsNotFoundException ex)
        {
            _logger.LogWarning(ex, "Settings not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ========== Rep Settings (Any Rep) ==========

    /// <summary>
    /// Gets rep settings (Dark Mode, Notifications, etc.).
    /// Creates default if none exist.
    /// </summary>
    [HttpGet("business-rep/{businessRepId:guid}/settings")]
    public async Task<IActionResult> GetRepSettings(Guid businessRepId)
    {
        try
        {
            var result = await _service.GetRepSettingsAsync(businessRepId);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            _logger.LogWarning(ex, "Business rep not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates rep settings (Dark Mode, Notifications, etc.).
    /// Rep can only modify their own settings.
    /// </summary>
    [HttpPatch("business-rep/{businessRepId:guid}/settings")]
    public async Task<IActionResult> UpdateRepSettings(
        Guid businessRepId,
        [FromBody] UpdateRepSettingsRequest request,
        [FromQuery] Guid? currentUserId)
    {
        try
        {
            var userId = currentUserId ?? Guid.Empty;
            if (userId == Guid.Empty)
                return BadRequest(new { error = "Current user ID is required" });

            var result = await _service.UpdateRepSettingsAsync(businessRepId, request, userId);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            _logger.LogWarning(ex, "Business rep not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedSettingsAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access: {Message}", ex.Message);
            return StatusCode(403, new { error = ex.Message });
        }
    }

    // ========== Combined View ==========

    /// <summary>
    /// Gets effective settings for a business rep (business + rep combined).
    /// </summary>
    [HttpGet("business-rep/{businessRepId:guid}/effective-settings")]
    public async Task<IActionResult> GetEffectiveSettings(Guid businessRepId)
    {
        try
        {
            var result = await _service.GetEffectiveSettingsAsync(businessRepId);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            _logger.LogWarning(ex, "Business rep not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }
}