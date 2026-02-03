using BusinessService.Application.DTOs.AutoResponse;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessAutoResponseController : ControllerBase
{
    private readonly IBusinessAutoResponseService _autoResponseService;
    private readonly ILogger<BusinessAutoResponseController> _logger;

    public BusinessAutoResponseController(
        IBusinessAutoResponseService autoResponseService,
        ILogger<BusinessAutoResponseController> logger)
    {
        _autoResponseService = autoResponseService;
        _logger = logger;
    }

    /// <summary>
    /// Get auto-response settings for a business
    /// </summary>
    [HttpGet("{businessId:guid}")]
    public async Task<ActionResult<BusinessAutoResponseDto>> GetAutoResponse(Guid businessId)
    {
        try
        {
            var result = await _autoResponseService.GetByBusinessIdAsync(businessId);
            if (result == null)
                return NotFound(new { message = $"Auto response settings for business {businessId} not found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting auto-response settings for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred while fetching auto-response settings" });
        }
    }

    /// <summary>
    /// Update auto-response settings for a business
    /// </summary>
    [HttpPatch("{businessId:guid}")]
    public async Task<ActionResult<BusinessAutoResponseDto>> UpdateAutoResponse(
        Guid businessId,
        [FromBody] UpdateBusinessAutoResponseRequest request)
    {
        try
        {
            var result = await _autoResponseService.UpdateAsync(businessId, request);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating auto-response settings for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred while updating auto-response settings" });
        }
    }
}
