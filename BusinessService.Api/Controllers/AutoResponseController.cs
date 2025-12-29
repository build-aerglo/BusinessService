using BusinessService.Application.DTOs.AutoResponse;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutoResponseController : ControllerBase
{
    private readonly IAutoResponseService _autoResponseService;
    private readonly ILogger<AutoResponseController> _logger;

    public AutoResponseController(
        IAutoResponseService autoResponseService,
        ILogger<AutoResponseController> logger)
    {
        _autoResponseService = autoResponseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all templates for a business
    /// </summary>
    [HttpGet("business/{businessId:guid}/templates")]
    public async Task<ActionResult<List<AutoResponseTemplateDto>>> GetTemplates(Guid businessId)
    {
        try
        {
            var templates = await _autoResponseService.GetTemplatesAsync(businessId);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get a specific template
    /// </summary>
    [HttpGet("templates/{templateId:guid}")]
    public async Task<ActionResult<AutoResponseTemplateDto>> GetTemplate(Guid templateId)
    {
        try
        {
            var template = await _autoResponseService.GetTemplateByIdAsync(templateId);
            if (template == null)
                return NotFound(new { message = "Template not found" });
            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template {TemplateId}", templateId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get auto-response settings for a business
    /// </summary>
    [HttpGet("business/{businessId:guid}/settings")]
    public async Task<ActionResult<AutoResponseSettingsDto>> GetSettings(Guid businessId)
    {
        try
        {
            var settings = await _autoResponseService.GetAutoResponseSettingsAsync(businessId);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Create a new template
    /// </summary>
    [HttpPost("templates")]
    public async Task<ActionResult<AutoResponseTemplateDto>> CreateTemplate(
        [FromBody] CreateAutoResponseTemplateRequest request,
        [FromHeader(Name = "X-User-Id")] Guid createdByUserId)
    {
        try
        {
            var template = await _autoResponseService.CreateTemplateAsync(request, createdByUserId);
            return CreatedAtAction(nameof(GetTemplate), new { templateId = template.Id }, template);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AutoResponseTemplateAlreadyExistsException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Update a template
    /// </summary>
    [HttpPut("templates")]
    public async Task<ActionResult<AutoResponseTemplateDto>> UpdateTemplate([FromBody] UpdateAutoResponseTemplateRequest request)
    {
        try
        {
            var template = await _autoResponseService.UpdateTemplateAsync(request);
            return Ok(template);
        }
        catch (AutoResponseTemplateNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", request.Id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Delete a template
    /// </summary>
    [HttpDelete("templates/{templateId:guid}")]
    public async Task<IActionResult> DeleteTemplate(Guid templateId)
    {
        try
        {
            await _autoResponseService.DeleteTemplateAsync(templateId);
            return Ok(new { message = "Template deleted successfully" });
        }
        catch (AutoResponseTemplateNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Set a template as default
    /// </summary>
    [HttpPut("templates/{templateId:guid}/set-default")]
    public async Task<IActionResult> SetDefaultTemplate(Guid templateId, [FromQuery] Guid businessId)
    {
        try
        {
            await _autoResponseService.SetDefaultTemplateAsync(templateId, businessId);
            return Ok(new { message = "Template set as default" });
        }
        catch (AutoResponseTemplateNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default template {TemplateId}", templateId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Generate an auto-response for a review
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<GeneratedAutoResponse>> GenerateResponse([FromBody] GenerateAutoResponseRequest request)
    {
        try
        {
            var response = await _autoResponseService.GenerateResponseAsync(request);
            if (response == null)
                return NotFound(new { message = "No matching template found" });
            return Ok(response);
        }
        catch (AutoResponseDisabledException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Preview a template with sample data
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<TemplatePreviewResponse>> PreviewTemplate([FromBody] PreviewTemplateRequest request)
    {
        try
        {
            var preview = await _autoResponseService.PreviewTemplateAsync(request);
            return Ok(preview);
        }
        catch (AutoResponseTemplateNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing template {TemplateId}", request.TemplateId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Enable auto-response for a business
    /// </summary>
    [HttpPost("business/{businessId:guid}/enable")]
    public async Task<IActionResult> EnableAutoResponse(
        Guid businessId,
        [FromHeader(Name = "X-User-Id")] Guid enabledByUserId)
    {
        try
        {
            await _autoResponseService.EnableAutoResponseAsync(businessId, enabledByUserId);
            return Ok(new { message = "Auto-response enabled" });
        }
        catch (BusinessSettingsNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling auto-response for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Disable auto-response for a business
    /// </summary>
    [HttpPost("business/{businessId:guid}/disable")]
    public async Task<IActionResult> DisableAutoResponse(
        Guid businessId,
        [FromHeader(Name = "X-User-Id")] Guid disabledByUserId)
    {
        try
        {
            await _autoResponseService.DisableAutoResponseAsync(businessId, disabledByUserId);
            return Ok(new { message = "Auto-response disabled" });
        }
        catch (BusinessSettingsNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling auto-response for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
