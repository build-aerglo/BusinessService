using BusinessService.Application.DTOs.ExternalSource;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalSourceController : ControllerBase
{
    private readonly IExternalSourceService _sourceService;
    private readonly ILogger<ExternalSourceController> _logger;

    public ExternalSourceController(
        IExternalSourceService sourceService,
        ILogger<ExternalSourceController> logger)
    {
        _sourceService = sourceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all external sources for a business
    /// </summary>
    [HttpGet("business/{businessId:guid}")]
    public async Task<ActionResult<ExternalSourcesListResponse>> GetExternalSources(Guid businessId)
    {
        try
        {
            var sources = await _sourceService.GetExternalSourcesAsync(businessId);
            return Ok(sources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sources for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get a specific external source
    /// </summary>
    [HttpGet("{sourceId:guid}")]
    public async Task<ActionResult<ExternalSourceDto>> GetSource(Guid sourceId)
    {
        try
        {
            var source = await _sourceService.GetExternalSourceByIdAsync(sourceId);
            if (source == null)
                return NotFound(new { message = "Source not found" });
            return Ok(source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting source {SourceId}", sourceId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get available source types
    /// </summary>
    [HttpGet("available-types")]
    public async Task<ActionResult<List<AvailableSourceTypeDto>>> GetAvailableTypes()
    {
        try
        {
            var types = await _sourceService.GetAvailableSourceTypesAsync();
            return Ok(types);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available source types");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Connect an external source
    /// </summary>
    [HttpPost("connect")]
    public async Task<ActionResult<ExternalSourceDto>> ConnectSource(
        [FromBody] ConnectExternalSourceRequest request,
        [FromHeader(Name = "X-User-Id")] Guid connectedByUserId)
    {
        try
        {
            var source = await _sourceService.ConnectSourceAsync(request, connectedByUserId);
            return CreatedAtAction(nameof(GetSource), new { sourceId = source.Id }, source);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ExternalSourceAlreadyConnectedException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ExternalSourceLimitExceededException ex)
        {
            return BadRequest(new { message = ex.Message, maxSources = ex.MaxSources, currentSources = ex.CurrentSources });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting source for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Disconnect an external source
    /// </summary>
    [HttpPost("{sourceId:guid}/disconnect")]
    public async Task<IActionResult> DisconnectSource(Guid sourceId)
    {
        try
        {
            await _sourceService.DisconnectSourceAsync(sourceId);
            return Ok(new { message = "Source disconnected successfully" });
        }
        catch (ExternalSourceNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting source {SourceId}", sourceId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Trigger a manual sync
    /// </summary>
    [HttpPost("{sourceId:guid}/sync")]
    public async Task<ActionResult<SyncResultResponse>> TriggerSync(Guid sourceId)
    {
        try
        {
            var result = await _sourceService.TriggerSyncAsync(sourceId);
            return Ok(result);
        }
        catch (ExternalSourceNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ExternalSourceSyncException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering sync for source {SourceId}", sourceId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Update sync settings
    /// </summary>
    [HttpPut("sync-settings")]
    public async Task<ActionResult<ExternalSourceDto>> UpdateSyncSettings([FromBody] UpdateSyncSettingsRequest request)
    {
        try
        {
            var source = await _sourceService.UpdateSyncSettingsAsync(request);
            return Ok(source);
        }
        catch (ExternalSourceNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sync settings for source {SourceId}", request.SourceId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Import reviews from CSV
    /// </summary>
    [HttpPost("import-csv")]
    public async Task<ActionResult<CsvUploadResult>> ImportFromCsv([FromBody] CsvUploadRequest request)
    {
        try
        {
            var result = await _sourceService.ImportFromCsvAsync(request);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CSV for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Check if more sources can be connected
    /// </summary>
    [HttpGet("business/{businessId:guid}/can-connect")]
    public async Task<ActionResult<object>> CanConnectMore(Guid businessId)
    {
        try
        {
            var canConnect = await _sourceService.CanConnectMoreSourcesAsync(businessId);
            return Ok(new { canConnect });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking connection limit for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
