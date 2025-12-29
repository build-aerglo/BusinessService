using BusinessService.Domain.Entities;

namespace BusinessService.Application.DTOs.AutoResponse;

/// <summary>
/// Auto-response template DTO
/// </summary>
public record AutoResponseTemplateDto(
    Guid Id,
    Guid BusinessId,
    string Name,
    ReviewSentiment Sentiment,
    string SentimentName,
    string TemplateContent,
    bool IsActive,
    bool IsDefault,
    int Priority,
    int? MinStarRating,
    int? MaxStarRating,
    int TimesUsed,
    DateTime? LastUsedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Request to create an auto-response template
/// </summary>
public class CreateAutoResponseTemplateRequest
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = default!;
    public ReviewSentiment Sentiment { get; set; }
    public string TemplateContent { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public int Priority { get; set; } = 1;
    public int? MinStarRating { get; set; }
    public int? MaxStarRating { get; set; }
}

/// <summary>
/// Request to update an auto-response template
/// </summary>
public class UpdateAutoResponseTemplateRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? TemplateContent { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDefault { get; set; }
    public int? Priority { get; set; }
    public int? MinStarRating { get; set; }
    public int? MaxStarRating { get; set; }
}

/// <summary>
/// Request to preview a populated template
/// </summary>
public class PreviewTemplateRequest
{
    public Guid TemplateId { get; set; }
    public string ReviewerName { get; set; } = default!;
    public string BusinessName { get; set; } = default!;
    public int StarRating { get; set; }
}

/// <summary>
/// Preview response
/// </summary>
public record TemplatePreviewResponse(
    Guid TemplateId,
    string OriginalContent,
    string PopulatedContent
);

/// <summary>
/// Auto-response settings DTO
/// </summary>
public record AutoResponseSettingsDto(
    Guid BusinessId,
    bool IsEnabled,
    DateTime? EnabledAt,
    int TotalTemplates,
    int ActiveTemplates,
    AutoResponseTemplateDto? DefaultPositiveTemplate,
    AutoResponseTemplateDto? DefaultNegativeTemplate,
    AutoResponseTemplateDto? DefaultNeutralTemplate
);

/// <summary>
/// Request to generate auto-response for a review
/// </summary>
public class GenerateAutoResponseRequest
{
    public Guid BusinessId { get; set; }
    public Guid ReviewId { get; set; }
    public string ReviewerName { get; set; } = default!;
    public int StarRating { get; set; }
    public ReviewSentiment Sentiment { get; set; }
}

/// <summary>
/// Generated auto-response
/// </summary>
public record GeneratedAutoResponse(
    Guid TemplateId,
    string TemplateName,
    string ResponseContent,
    bool RequiresApproval
);
