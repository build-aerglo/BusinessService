using BusinessService.Application.DTOs.Analytics;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessAnalyticsController : ControllerBase
{
    private readonly IBusinessAnalyticsService _analyticsService;
    private readonly ILogger<BusinessAnalyticsController> _logger;

    public BusinessAnalyticsController(
        IBusinessAnalyticsService analyticsService,
        ILogger<BusinessAnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get analytics dashboard for a business
    /// </summary>
    [HttpGet("business/{businessId:guid}/dashboard")]
    public async Task<ActionResult<AnalyticsDashboardDto>> GetDashboard(Guid businessId)
    {
        try
        {
            var dashboard = await _analyticsService.GetDashboardAsync(businessId);
            return Ok(dashboard);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get latest analytics for a business
    /// </summary>
    [HttpGet("business/{businessId:guid}/latest")]
    public async Task<ActionResult<BusinessAnalyticsDto>> GetLatest(Guid businessId)
    {
        try
        {
            var analytics = await _analyticsService.GetLatestAnalyticsAsync(businessId);
            if (analytics == null)
                return NotFound(new { message = "No analytics found" });
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest analytics for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get analytics history for a business
    /// </summary>
    [HttpGet("business/{businessId:guid}/history")]
    public async Task<ActionResult<List<BusinessAnalyticsDto>>> GetHistory(
        Guid businessId,
        [FromQuery] int limit = 12)
    {
        try
        {
            var history = await _analyticsService.GetAnalyticsHistoryAsync(businessId, limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics history for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Generate analytics for a specific period
    /// </summary>
    [HttpPost("business/{businessId:guid}/generate")]
    public async Task<ActionResult<BusinessAnalyticsDto>> GenerateAnalytics(
        Guid businessId,
        [FromQuery] AnalyticsPeriodType periodType = AnalyticsPeriodType.Monthly)
    {
        try
        {
            var analytics = await _analyticsService.GenerateAnalyticsAsync(businessId, periodType);
            return Ok(analytics);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating analytics for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get branch comparison (Enterprise only)
    /// </summary>
    [HttpGet("business/{parentBusinessId:guid}/branch-comparison")]
    public async Task<ActionResult<BranchComparisonDto>> GetBranchComparison(Guid parentBusinessId)
    {
        try
        {
            var comparison = await _analyticsService.GetBranchComparisonAsync(parentBusinessId);
            if (comparison == null)
                return NotFound(new { message = "No branch comparison found" });
            return Ok(comparison);
        }
        catch (FeatureNotAvailableException ex)
        {
            return StatusCode(403, new { message = ex.Message, requiredPlan = ex.RequiredPlan });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branch comparison for business {BusinessId}", parentBusinessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Generate branch comparison (Enterprise only)
    /// </summary>
    [HttpPost("business/{parentBusinessId:guid}/branch-comparison/generate")]
    public async Task<ActionResult<BranchComparisonDto>> GenerateBranchComparison(Guid parentBusinessId)
    {
        try
        {
            var comparison = await _analyticsService.GenerateBranchComparisonAsync(parentBusinessId);
            return Ok(comparison);
        }
        catch (FeatureNotAvailableException ex)
        {
            return StatusCode(403, new { message = ex.Message, requiredPlan = ex.RequiredPlan });
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating branch comparison for business {BusinessId}", parentBusinessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get competitor comparison (Enterprise only)
    /// </summary>
    [HttpGet("business/{businessId:guid}/competitor-comparison")]
    public async Task<ActionResult<CompetitorComparisonDto>> GetCompetitorComparison(Guid businessId)
    {
        try
        {
            var comparison = await _analyticsService.GetCompetitorComparisonAsync(businessId);
            if (comparison == null)
                return NotFound(new { message = "No competitor comparison found. Add competitors first." });
            return Ok(comparison);
        }
        catch (FeatureNotAvailableException ex)
        {
            return StatusCode(403, new { message = ex.Message, requiredPlan = ex.RequiredPlan });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting competitor comparison for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Add a competitor for comparison (Enterprise only)
    /// </summary>
    [HttpPost("competitors")]
    public async Task<IActionResult> AddCompetitor(
        [FromBody] AddCompetitorRequest request,
        [FromHeader(Name = "X-User-Id")] Guid addedByUserId)
    {
        try
        {
            await _analyticsService.AddCompetitorAsync(request, addedByUserId);
            return Ok(new { message = "Competitor added successfully" });
        }
        catch (FeatureNotAvailableException ex)
        {
            return StatusCode(403, new { message = ex.Message, requiredPlan = ex.RequiredPlan });
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidSubscriptionOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (SubscriptionLimitExceededException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding competitor for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Remove a competitor from comparison
    /// </summary>
    [HttpDelete("competitors")]
    public async Task<IActionResult> RemoveCompetitor([FromBody] RemoveCompetitorRequest request)
    {
        try
        {
            await _analyticsService.RemoveCompetitorAsync(request);
            return Ok(new { message = "Competitor removed successfully" });
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing competitor for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Check branch comparison availability
    /// </summary>
    [HttpGet("business/{businessId:guid}/can-access-branch-comparison")]
    public async Task<ActionResult<object>> CanAccessBranchComparison(Guid businessId)
    {
        try
        {
            var canAccess = await _analyticsService.CanAccessBranchComparisonAsync(businessId);
            return Ok(new { canAccess, feature = "branch_comparison" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking branch comparison access for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Check competitor comparison availability
    /// </summary>
    [HttpGet("business/{businessId:guid}/can-access-competitor-comparison")]
    public async Task<ActionResult<object>> CanAccessCompetitorComparison(Guid businessId)
    {
        try
        {
            var canAccess = await _analyticsService.CanAccessCompetitorComparisonAsync(businessId);
            return Ok(new { canAccess, feature = "competitor_comparison" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking competitor comparison access for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
