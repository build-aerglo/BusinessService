namespace BusinessService.Application.DTOs.Settings;

/// <summary>
/// Auto-response templates for different review sentiments
/// </summary>
public record AutoResponseTemplatesDto(
    string? PositiveTemplate,
    string? NegativeTemplate,
    string? NeutralTemplate
);
