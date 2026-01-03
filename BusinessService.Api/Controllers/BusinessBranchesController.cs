using BusinessService.Application.DTOs;
using BusinessService.Application.DTOs.Settings;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/business-branch")]
public class BusinessBranchesController : ControllerBase
{
    private readonly IBusinessService _service;
    private readonly ILogger<BusinessBranchesController> _logger;

    public BusinessBranchesController(
        IBusinessService service,
        ILogger<BusinessBranchesController> logger)
    {
        _service = service;
        _logger = logger;
    }
    
    /// <summary>
    /// Creates branch
    /// </summary>
    [HttpPost()]
    public async Task<IActionResult> CreateBranch(BranchDto dto)
    {
        try
        {
            var res = await _service.AddBranchesAsync(dto);
            return Ok(res);
        }
        catch (BusinessNotFoundException ex)
        {
            _logger.LogWarning(ex, "Business not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (BranchNotFoundException ex)
        {
            _logger.LogWarning(ex, "Error creating branch: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get Business Branches
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBusinessBranches(Guid id)
    {
        try
        {
            var result = await _service.GetBusinessBranchesAsync(id);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            _logger.LogWarning(ex, "Business not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Delete Business Branche
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBranch(Guid id)
    {
        try
        {
            await _service.DeleteBranchesAsync(id);
            return Ok(new {message = "Branch deleted successfully."});
        }
        catch (BranchNotFoundException ex)
        {
            _logger.LogWarning(ex, "Branch not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }
    
    [HttpPatch()]
    public async Task<IActionResult> UpdateBranch(BranchUpdateDto dto)
    {
        try
        {
            await _service.UpdateBranchesAsync(dto);
            return Ok(new {message = "Branch updated successfully."});
        }
        catch (BranchNotFoundException ex)
        {
            _logger.LogWarning(ex, "Branch not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }
    
    [HttpGet("branch/{branchId:guid}")]
    public async Task<IActionResult> GetBranchById(Guid branchId)
    {
        try
        {
            var branch = await _service.GetBranchByIdAsync(branchId);
            return Ok(branch);
        }
        catch (BranchNotFoundException ex)
        {
            _logger.LogWarning(ex, "Branch not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }
}