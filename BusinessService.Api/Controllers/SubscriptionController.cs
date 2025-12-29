using BusinessService.Application.DTOs.Subscription;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available subscription plans
    /// </summary>
    [HttpGet("plans")]
    public async Task<ActionResult<List<SubscriptionPlanDto>>> GetAllPlans()
    {
        try
        {
            var plans = await _subscriptionService.GetAllPlansAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return StatusCode(500, new { message = "An error occurred while fetching plans" });
        }
    }

    /// <summary>
    /// Get a specific subscription plan
    /// </summary>
    [HttpGet("plans/{planId:guid}")]
    public async Task<ActionResult<SubscriptionPlanDto>> GetPlan(Guid planId)
    {
        try
        {
            var plan = await _subscriptionService.GetPlanByIdAsync(planId);
            if (plan == null)
                return NotFound(new { message = "Plan not found" });
            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plan {PlanId}", planId);
            return StatusCode(500, new { message = "An error occurred while fetching the plan" });
        }
    }

    /// <summary>
    /// Get business subscription
    /// </summary>
    [HttpGet("business/{businessId:guid}")]
    public async Task<ActionResult<BusinessSubscriptionDto>> GetBusinessSubscription(Guid businessId)
    {
        try
        {
            var subscription = await _subscriptionService.GetBusinessSubscriptionAsync(businessId);
            if (subscription == null)
                return NotFound(new { message = "No active subscription found" });
            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred while fetching the subscription" });
        }
    }

    /// <summary>
    /// Create a new subscription
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BusinessSubscriptionDto>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.CreateSubscriptionAsync(request);
            return CreatedAtAction(nameof(GetBusinessSubscription), new { businessId = request.BusinessId }, subscription);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (SubscriptionNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidSubscriptionOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred while creating the subscription" });
        }
    }

    /// <summary>
    /// Upgrade subscription
    /// </summary>
    [HttpPut("upgrade")]
    public async Task<ActionResult<BusinessSubscriptionDto>> UpgradeSubscription([FromBody] UpgradeSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.UpgradeSubscriptionAsync(request);
            return Ok(subscription);
        }
        catch (SubscriptionNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred while upgrading the subscription" });
        }
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    [HttpDelete("cancel")]
    public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionRequest request)
    {
        try
        {
            await _subscriptionService.CancelSubscriptionAsync(request);
            return Ok(new { message = "Subscription cancelled successfully" });
        }
        catch (SubscriptionNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred while cancelling the subscription" });
        }
    }

    /// <summary>
    /// Get subscription usage
    /// </summary>
    [HttpGet("business/{businessId:guid}/usage")]
    public async Task<ActionResult<SubscriptionUsageDto>> GetUsage(Guid businessId)
    {
        try
        {
            var usage = await _subscriptionService.GetUsageAsync(businessId);
            return Ok(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred while fetching usage" });
        }
    }

    /// <summary>
    /// Check if an action can be performed
    /// </summary>
    [HttpGet("business/{businessId:guid}/can-perform/{actionType}")]
    public async Task<ActionResult<bool>> CanPerformAction(Guid businessId, string actionType)
    {
        try
        {
            var canPerform = await _subscriptionService.CanPerformActionAsync(businessId, actionType);
            return Ok(new { canPerform, actionType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking action availability for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Check feature availability
    /// </summary>
    [HttpGet("business/{businessId:guid}/feature/{featureName}")]
    public async Task<ActionResult<FeatureAvailabilityResponse>> CheckFeature(Guid businessId, string featureName)
    {
        try
        {
            var availability = await _subscriptionService.CheckFeatureAvailabilityAsync(businessId, featureName);
            return Ok(availability);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature availability for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get upgrade comparison
    /// </summary>
    [HttpGet("business/{businessId:guid}/upgrade-comparison")]
    public async Task<ActionResult<SubscriptionComparisonDto>> GetUpgradeComparison(Guid businessId)
    {
        try
        {
            var comparison = await _subscriptionService.GetUpgradeComparisonAsync(businessId);
            if (comparison == null)
                return NotFound(new { message = "No upgrade available" });
            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upgrade comparison for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
