using BusinessService.Application.DTOs.AutoResponse;
using BusinessService.Domain.Entities;

namespace BusinessService.Application.Interfaces;

public interface IAutoResponseService
{
    // Template management
    Task<List<AutoResponseTemplateDto>> GetTemplatesAsync(Guid businessId);
    Task<AutoResponseTemplateDto?> GetTemplateByIdAsync(Guid templateId);
    Task<AutoResponseTemplateDto> CreateTemplateAsync(CreateAutoResponseTemplateRequest request, Guid createdByUserId);
    Task<AutoResponseTemplateDto> UpdateTemplateAsync(UpdateAutoResponseTemplateRequest request);
    Task DeleteTemplateAsync(Guid templateId);

    // Default templates
    Task<AutoResponseSettingsDto> GetAutoResponseSettingsAsync(Guid businessId);
    Task CreateDefaultTemplatesAsync(Guid businessId);
    Task SetDefaultTemplateAsync(Guid templateId, Guid businessId);

    // Auto-response generation
    Task<GeneratedAutoResponse?> GenerateResponseAsync(GenerateAutoResponseRequest request);
    Task<TemplatePreviewResponse> PreviewTemplateAsync(PreviewTemplateRequest request);

    // Feature toggle
    Task EnableAutoResponseAsync(Guid businessId, Guid enabledByUserId);
    Task DisableAutoResponseAsync(Guid businessId, Guid disabledByUserId);
}
