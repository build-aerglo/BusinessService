using BusinessService.Application.DTOs.Analytics;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BusinessService.Api.Controllers;

/// <summary>
/// Analytics endpoints for BusinessService.
///
/// ARCHITECTURE:
///   - GET /dashboard and GET /latest read from business_analytics table
///     populated by the Azure Function (AnalyticsProcessorFunction).
///   - Enterprise branch/competitor endpoints still use IBusinessAnalyticsService directly.
///   - POST /generate has been removed — the Azure Function runs every 5 minutes
///     automatically. Use POST /api/trigger on the Function App to force a run.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BusinessAnalyticsController : ControllerBase
{
    private readonly IAnalyticsReadRepository _analyticsReadRepo;
    private readonly IBusinessAnalyticsService _analyticsService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BusinessAnalyticsController> _logger;

    public BusinessAnalyticsController(
        IAnalyticsReadRepository analyticsReadRepo,
        IBusinessAnalyticsService analyticsService,
        IMemoryCache cache,
        ILogger<BusinessAnalyticsController> logger)
    {
        _analyticsReadRepo = analyticsReadRepo;
        _analyticsService  = analyticsService;
        _cache             = cache;
        _logger            = logger;
    }

    // ============================================================
    // STANDARD — reads pre-calculated data from Azure Function output
    // ============================================================

    /// <summary>
    /// Get analytics dashboard for a business (cached 5 minutes).
    /// Data is pre-calculated by the Azure Function every 5 minutes.
    /// Returns 404 if the Function has not yet run for this business.
    /// </summary>
    [HttpGet("business/{businessId:guid}/dashboard")]
    public async Task<IActionResult> GetDashboard(Guid businessId)
    {
        var cacheKey = $"analytics_dashboard_{businessId}";

        if (_cache.TryGetValue(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("Returning cached dashboard for business {BusinessId}.", businessId);
            return Ok(cached);
        }

        try
        {
            var analytics = await _analyticsReadRepo.GetDashboardAsync(businessId);

            if (analytics == null)
            {
                return NotFound(new
                {
                    message =
                        "No analytics data found for this business. " +
                        "The analytics function runs every 5 minutes. " +
                        "If this business has no reviews yet, data will appear after the first review is approved.",
                    businessId
                });
            }

            _cache.Set(cacheKey, analytics, TimeSpan.FromMinutes(5));
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard for business {BusinessId}.", businessId);
            return StatusCode(500, new { message = "An error occurred while fetching analytics." });
        }
    }

    /// <summary>
    /// Get latest analytics for a business, always fetching fresh from DB (no cache).
    /// </summary>
    [HttpGet("business/{businessId:guid}/latest")]
    public async Task<IActionResult> GetLatest(Guid businessId)
    {
        try
        {
            var analytics = await _analyticsReadRepo.GetDashboardAsync(businessId);

            if (analytics == null)
                return NotFound(new { message = "No analytics found for this business.", businessId });

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest analytics for business {BusinessId}.", businessId);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    // ============================================================
    // ENTERPRISE — branch comparison (unchanged)
    // ============================================================

    [HttpGet("business/{parentBusinessId:guid}/branch-comparison")]
    public async Task<IActionResult> GetBranchComparison(Guid parentBusinessId)
    {
        try
        {
            var comparison = await _analyticsService.GetBranchComparisonAsync(parentBusinessId);
            if (comparison == null) return NotFound(new { message = "No branch comparison found." });
            return Ok(comparison);
        }
        catch (FeatureNotAvailableException ex)
        { return StatusCode(403, new { message = ex.Message, requiredPlan = ex.RequiredPlan }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branch comparison for {BusinessId}.", parentBusinessId);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    [HttpPost("business/{parentBusinessId:guid}/branch-comparison/generate")]
    public async Task<IActionResult> GenerateBranchComparison(Guid parentBusinessId)
    {
        try
        {
            var comparison = await _analyticsService.GenerateBranchComparisonAsync(parentBusinessId);
            return Ok(comparison);
        }
        catch (FeatureNotAvailableException ex)
        { return StatusCode(403, new { message = ex.Message, requiredPlan = ex.RequiredPlan }); }
        catch (BusinessNotFoundException ex)
        { return NotFound(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating branch comparison for {BusinessId}.", parentBusinessId);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    // ============================================================
    // ENTERPRISE — competitor comparison (unchanged)
    // ============================================================

    [HttpGet("business/{businessId:guid}/competitor-comparison")]
    public async Task<IActionResult> GetCompetitorComparison(Guid businessId)
    {
        try
        {
            var comparison = await _analyticsService.GetCompetitorComparisonAsync(businessId);
            if (comparison == null)
                return NotFound(new { message = "No competitor comparison found. Add competitors first." });
            return Ok(comparison);
        }
        catch (FeatureNotAvailableException ex)
        { return StatusCode(403, new { message = ex.Message, requiredPlan = ex.RequiredPlan }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting competitor comparison for {BusinessId}.", businessId);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    [HttpPost("competitors")]
    public async Task<IActionResult> AddCompetitor(
        [FromBody] AddCompetitorRequest request,
        [FromHeader(Name = "X-User-Id")] Guid addedByUserId)
    {
        try
        {
            await _analyticsService.AddCompetitorAsync(request, addedByUserId);
            return Ok(new { message = "Competitor added successfully." });
        }
        catch (FeatureNotAvailableException ex)
        { return StatusCode(403, new { message = ex.Message, requiredPlan = ex.RequiredPlan }); }
        catch (BusinessNotFoundException ex)
        { return NotFound(new { message = ex.Message }); }
        catch (InvalidSubscriptionOperationException ex)
        { return Conflict(new { message = ex.Message }); }
        catch (SubscriptionLimitExceededException ex)
        { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding competitor for {BusinessId}.", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    [HttpDelete("competitors")]
    public async Task<IActionResult> RemoveCompetitor([FromBody] RemoveCompetitorRequest request)
    {
        try
        {
            await _analyticsService.RemoveCompetitorAsync(request);
            return Ok(new { message = "Competitor removed successfully." });
        }
        catch (BusinessNotFoundException ex)
        { return NotFound(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing competitor for {BusinessId}.", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    [HttpGet("business/{businessId:guid}/can-access-branch-comparison")]
    public async Task<IActionResult> CanAccessBranchComparison(Guid businessId)
    {
        try
        {
            var canAccess = await _analyticsService.CanAccessBranchComparisonAsync(businessId);
            return Ok(new { canAccess, feature = "branch_comparison" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking branch comparison access for {BusinessId}.", businessId);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    [HttpGet("business/{businessId:guid}/can-access-competitor-comparison")]
    public async Task<IActionResult> CanAccessCompetitorComparison(Guid businessId)
    {
        try
        {
            var canAccess = await _analyticsService.CanAccessCompetitorComparisonAsync(businessId);
            return Ok(new { canAccess, feature = "competitor_comparison" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking competitor comparison access for {BusinessId}.", businessId);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }
}