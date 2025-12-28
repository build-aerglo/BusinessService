using BusinessService.Application.DTOs;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessController : ControllerBase
{
    private readonly IBusinessService _service;
    private readonly ILogger<BusinessController> _logger;

    public BusinessController(IBusinessService service, ILogger<BusinessController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new business.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessRequest request)
    {
        try
        {
            var result = await _service.CreateBusinessAsync(request);
            return CreatedAtAction(nameof(GetBusiness), new { id = result.Id }, result);
        }
        catch (BusinessAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "Business creation failed: {Message}", ex.Message);
            return Conflict(new { error = ex.Message });
        }
        catch (CategoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invalid category reference: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a business by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBusiness(Guid id)
    {
        try
        {
            var result = await _service.GetBusinessAsync(id);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            _logger.LogWarning(ex, "Business not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates a business’s ratings (used internally).
    /// </summary>
    [HttpPatch("{id:guid}/ratings")]
    public async Task<IActionResult> UpdateRatings(Guid id, [FromQuery] decimal newAverage, [FromQuery] long newCount)
    {
        try
        {
            await _service.UpdateRatingsAsync(id, newAverage, newCount);
            return NoContent();
        }
        catch (BusinessNotFoundException ex)
        {
            _logger.LogWarning(ex, "Business not found during rating update: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates a business's profile.
    /// </summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateBusiness(Guid id, [FromBody] UpdateBusinessRequest request)
    {
        try
        {
            var result = await _service.UpdateBusinessAsync(id, request);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            _logger.LogWarning(ex, "Business not found during update: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessConflictException ex)
        {
            _logger.LogWarning(ex, "Business conflict during update: {Message}", ex.Message);
            return Conflict(new { error = ex.Message });
        }
    }
    
    [HttpGet("category/{categoryId}")]
    public async Task<IActionResult> GetBusinessesByCategory(Guid categoryId)
    {
        try
        {
            var results = await _service.GetBusinessesByCategoryAsync(categoryId);
            return Ok(results);
        }
        catch (CategoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Category not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }
    
    [HttpGet("by-tag/{tagId:guid}")]
    public async Task<IActionResult> GetBusinessesByTag(Guid tagId)
    {
        var businesses = await _service.GetBusinessesByTagAsync(tagId);
        return Ok(businesses);
    }
    
    /// <summary>
    /// Create a business claim.
    /// </summary>
    [HttpPost("claim-business")]
    public async Task<IActionResult> ClaimBusiness(BusinessClaimsDto dto)
    {
        try
        {
            await _service.ClaimBusinessAsync(dto);
            return Ok();
        }
        catch (BusinessNotFoundException ex)
        {
            _logger.LogWarning(ex, "Business not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessConflictException ex)
        {
            _logger.LogWarning(ex, "Business conflict during claim: {Message}", ex.Message);
            return Conflict(new { error = ex.Message });
        }
    }
}