using BusinessService.Application.DTOs;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _service;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ICategoryService service, ILogger<CategoryController> logger)
    {
        _service = service;
        _logger = logger;
    }
    
    /// <summary>
    /// Creates a new category.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var result = await _service.CreateCategoryAsync(request);
            return CreatedAtAction(nameof(GetCategory), new { id = result.Id }, result);
        }
        catch (CategoryAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "Category creation failed: {Message}", ex.Message);
            return Conflict(new { error = ex.Message });
        }
        catch (CategoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invalid parent category: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a category by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        try
        {
            var result = await _service.GetCategoryAsync(id);
            return Ok(result);
        }
        catch (CategoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Category not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all top-level categories (no parent).
    /// </summary>
    [HttpGet("top-level")]
    public async Task<IActionResult> GetTopLevelCategories()
    {
        var result = await _service.GetAllTopLevelCategoriesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all sub-categories under a given parent category.
    /// </summary>
    [HttpGet("{parentId:guid}/subcategories")]
    public async Task<IActionResult> GetSubCategories(Guid parentId)
    {
        try
        {
            var result = await _service.GetSubCategoriesAsync(parentId);
            return Ok(result);
        }
        catch (CategoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Parent category not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }
}
