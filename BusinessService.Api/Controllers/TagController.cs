using BusinessService.Application.DTOs;
using BusinessService.Application.DTOs.Settings;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagController : ControllerBase
{
    private readonly ITagService _service;
    private readonly ILogger<TagController> _logger;

    public TagController(ITagService service, ILogger<TagController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new tag.
    /// </summary>
    [HttpPost($"add-tag")]
    public async Task<IActionResult> CreateTag([FromBody] NewTagRequest request)
    {
        try
        {
            var result = await _service.CreateTagAsync(request);
            return Ok(result);
        }
        catch (CategoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Category does not exist: {Message}", ex.Message);
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error occured: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Retrieves category tags.
    /// </summary>
    [HttpGet("/get-category-tags/{id:guid}")]
    public async Task<IActionResult> GetCategoryTags(Guid id)
    {
        try
        {
            var result = await _service.GetCategoryTagsAsync(id);
            return Ok(result);
        }
        catch (CategoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Category not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }
    
    
}