using BusinessService.Application.DTOs.Claims;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessClaimController : ControllerBase
{
    private readonly IBusinessClaimService _claimService;
    private readonly ILogger<BusinessClaimController> _logger;

    public BusinessClaimController(
        IBusinessClaimService claimService,
        ILogger<BusinessClaimController> logger)
    {
        _claimService = claimService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a business claim
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BusinessClaimRequestDto>> SubmitClaim(
        [FromBody] SubmitBusinessClaimRequest request,
        [FromHeader(Name = "X-User-Id")] Guid? userId = null)
    {
        try
        {
            var claim = await _claimService.SubmitClaimAsync(request, userId);
            return CreatedAtAction(nameof(GetClaimById), new { claimId = claim.Id }, claim);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessAlreadyClaimedException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ClaimAlreadyExistsException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting claim for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get claim by ID
    /// </summary>
    [HttpGet("{claimId:guid}")]
    public async Task<ActionResult<BusinessClaimRequestDto>> GetClaimById(Guid claimId)
    {
        try
        {
            var claim = await _claimService.GetClaimByIdAsync(claimId);
            if (claim == null)
                return NotFound(new { message = "Claim not found" });
            return Ok(claim);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claim {ClaimId}", claimId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get claim status
    /// </summary>
    [HttpGet("{claimId:guid}/status")]
    public async Task<ActionResult<ClaimStatusResponse>> GetClaimStatus(Guid claimId)
    {
        try
        {
            var status = await _claimService.GetClaimStatusAsync(claimId);
            return Ok(status);
        }
        catch (ClaimNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claim status {ClaimId}", claimId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get claims for a business
    /// </summary>
    [HttpGet("business/{businessId:guid}")]
    public async Task<ActionResult<List<BusinessClaimRequestDto>>> GetClaimsByBusiness(Guid businessId)
    {
        try
        {
            var claims = await _claimService.GetClaimsByBusinessIdAsync(businessId);
            return Ok(claims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claims for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get all pending claims (admin)
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<PendingClaimsListResponse>> GetPendingClaims()
    {
        try
        {
            var claims = await _claimService.GetPendingClaimsAsync();
            return Ok(claims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending claims");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Review a claim (admin)
    /// </summary>
    [HttpPost("review")]
    public async Task<ActionResult<BusinessClaimRequestDto>> ReviewClaim([FromBody] ReviewClaimRequest request)
    {
        try
        {
            var claim = await _claimService.ReviewClaimAsync(request);
            return Ok(claim);
        }
        catch (ClaimNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidClaimOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing claim {ClaimId}", request.ClaimId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Escalate a claim
    /// </summary>
    [HttpPost("{claimId:guid}/escalate")]
    public async Task<ActionResult<BusinessClaimRequestDto>> EscalateClaim(Guid claimId, [FromBody] string reason)
    {
        try
        {
            var claim = await _claimService.EscalateClaimAsync(claimId, reason);
            return Ok(claim);
        }
        catch (ClaimNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating claim {ClaimId}", claimId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Check if business is claimed
    /// </summary>
    [HttpGet("business/{businessId:guid}/is-claimed")]
    public async Task<ActionResult<object>> IsBusinessClaimed(Guid businessId)
    {
        try
        {
            var isClaimed = await _claimService.IsBusinessClaimedAsync(businessId);
            var hasPending = await _claimService.HasPendingClaimAsync(businessId);
            return Ok(new { isClaimed, hasPendingClaim = hasPending });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking claim status for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get unclaimed businesses
    /// </summary>
    [HttpGet("unclaimed")]
    public async Task<ActionResult<List<UnclaimedBusinessDto>>> GetUnclaimedBusinesses(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        try
        {
            var businesses = await _claimService.GetUnclaimedBusinessesAsync(limit, offset);
            return Ok(businesses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unclaimed businesses");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
