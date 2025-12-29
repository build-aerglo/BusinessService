using BusinessService.Application.DTOs.Verification;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessVerificationController : ControllerBase
{
    private readonly IBusinessVerificationService _verificationService;
    private readonly ILogger<BusinessVerificationController> _logger;

    public BusinessVerificationController(
        IBusinessVerificationService verificationService,
        ILogger<BusinessVerificationController> logger)
    {
        _verificationService = verificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get verification status for a business
    /// </summary>
    [HttpGet("{businessId:guid}")]
    public async Task<ActionResult<BusinessVerificationDto>> GetVerificationStatus(Guid businessId)
    {
        try
        {
            var result = await _verificationService.GetVerificationStatusAsync(businessId);
            return Ok(result);
        }
        catch (VerificationNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting verification status for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred while fetching verification status" });
        }
    }

    /// <summary>
    /// Get detailed verification status with requirements
    /// </summary>
    [HttpGet("{businessId:guid}/detailed")]
    public async Task<ActionResult<VerificationStatusResponse>> GetDetailedStatus(Guid businessId)
    {
        try
        {
            var result = await _verificationService.GetDetailedStatusAsync(businessId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detailed verification status for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred while fetching detailed status" });
        }
    }

    /// <summary>
    /// Create verification record for a business
    /// </summary>
    [HttpPost("{businessId:guid}")]
    public async Task<ActionResult<BusinessVerificationDto>> CreateVerification(Guid businessId)
    {
        try
        {
            var result = await _verificationService.CreateVerificationAsync(businessId);
            return CreatedAtAction(nameof(GetVerificationStatus), new { businessId }, result);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (VerificationRequiredException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating verification for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred while creating verification" });
        }
    }

    /// <summary>
    /// Verify a specific requirement
    /// </summary>
    [HttpPost("verify")]
    public async Task<ActionResult<BusinessVerificationDto>> VerifyRequirement([FromBody] VerifyRequirementRequest request)
    {
        try
        {
            var result = await _verificationService.VerifyRequirementAsync(request);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying requirement for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred while verifying requirement" });
        }
    }

    /// <summary>
    /// Trigger re-verification for a business
    /// </summary>
    [HttpPost("{businessId:guid}/reverify")]
    public async Task<IActionResult> TriggerReverification(Guid businessId, [FromBody] string reason)
    {
        try
        {
            await _verificationService.TriggerReverificationAsync(businessId, reason);
            return Ok(new { message = "Re-verification triggered successfully" });
        }
        catch (VerificationNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering re-verification for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred while triggering re-verification" });
        }
    }

    /// <summary>
    /// Get all businesses requiring re-verification
    /// </summary>
    [HttpGet("requiring-reverification")]
    public async Task<ActionResult<List<BusinessVerificationDto>>> GetRequiringReverification()
    {
        try
        {
            var result = await _verificationService.GetBusinessesRequiringReverificationAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting businesses requiring re-verification");
            return StatusCode(500, new { message = "An error occurred while fetching businesses" });
        }
    }
}
