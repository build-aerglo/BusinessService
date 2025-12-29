using BusinessService.Application.DTOs.AutoResponse;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;

namespace BusinessService.Application.Services;

public class AutoResponseService : IAutoResponseService
{
    private readonly IAutoResponseTemplateRepository _templateRepository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IBusinessSettingsRepository _settingsRepository;

    public AutoResponseService(
        IAutoResponseTemplateRepository templateRepository,
        IBusinessRepository businessRepository,
        IBusinessSettingsRepository settingsRepository)
    {
        _templateRepository = templateRepository;
        _businessRepository = businessRepository;
        _settingsRepository = settingsRepository;
    }

    public async Task<List<AutoResponseTemplateDto>> GetTemplatesAsync(Guid businessId)
    {
        var templates = await _templateRepository.FindByBusinessIdAsync(businessId);
        return templates.Select(MapToDto).ToList();
    }

    public async Task<AutoResponseTemplateDto?> GetTemplateByIdAsync(Guid templateId)
    {
        var template = await _templateRepository.FindByIdAsync(templateId);
        return template != null ? MapToDto(template) : null;
    }

    public async Task<AutoResponseTemplateDto> CreateTemplateAsync(CreateAutoResponseTemplateRequest request, Guid createdByUserId)
    {
        var business = await _businessRepository.FindByIdAsync(request.BusinessId);
        if (business == null)
            throw new BusinessNotFoundException($"Business with ID {request.BusinessId} not found");

        if (await _templateRepository.ExistsByNameAndBusinessIdAsync(request.Name, request.BusinessId))
            throw new AutoResponseTemplateAlreadyExistsException($"Template with name '{request.Name}' already exists");

        var template = new AutoResponseTemplate
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            Name = request.Name,
            Sentiment = request.Sentiment,
            TemplateContent = request.TemplateContent,
            IsActive = request.IsActive,
            IsDefault = request.IsDefault,
            Priority = request.Priority,
            MinStarRating = request.MinStarRating,
            MaxStarRating = request.MaxStarRating,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _templateRepository.AddAsync(template);
        return MapToDto(template);
    }

    public async Task<AutoResponseTemplateDto> UpdateTemplateAsync(UpdateAutoResponseTemplateRequest request)
    {
        var template = await _templateRepository.FindByIdAsync(request.Id);
        if (template == null)
            throw new AutoResponseTemplateNotFoundException($"Template with ID {request.Id} not found");

        if (request.Name != null) template.Name = request.Name;
        if (request.TemplateContent != null) template.TemplateContent = request.TemplateContent;
        if (request.IsActive.HasValue) template.IsActive = request.IsActive.Value;
        if (request.IsDefault.HasValue) template.IsDefault = request.IsDefault.Value;
        if (request.Priority.HasValue) template.Priority = request.Priority.Value;
        if (request.MinStarRating.HasValue) template.MinStarRating = request.MinStarRating;
        if (request.MaxStarRating.HasValue) template.MaxStarRating = request.MaxStarRating;

        template.UpdatedAt = DateTime.UtcNow;
        await _templateRepository.UpdateAsync(template);
        return MapToDto(template);
    }

    public async Task DeleteTemplateAsync(Guid templateId)
    {
        var template = await _templateRepository.FindByIdAsync(templateId);
        if (template == null)
            throw new AutoResponseTemplateNotFoundException($"Template with ID {templateId} not found");

        await _templateRepository.DeleteAsync(templateId);
    }

    public async Task<AutoResponseSettingsDto> GetAutoResponseSettingsAsync(Guid businessId)
    {
        var settings = await _settingsRepository.FindBusinessSettingsByBusinessIdAsync(businessId);
        var templates = await _templateRepository.FindByBusinessIdAsync(businessId);

        var positiveDefault = templates.FirstOrDefault(t => t.Sentiment == ReviewSentiment.Positive && t.IsDefault);
        var negativeDefault = templates.FirstOrDefault(t => t.Sentiment == ReviewSentiment.Negative && t.IsDefault);
        var neutralDefault = templates.FirstOrDefault(t => t.Sentiment == ReviewSentiment.Neutral && t.IsDefault);

        return new AutoResponseSettingsDto(
            businessId,
            settings?.AutoResponseEnabled ?? false,
            settings?.AutoResponseEnabledAt,
            templates.Count,
            templates.Count(t => t.IsActive),
            positiveDefault != null ? MapToDto(positiveDefault) : null,
            negativeDefault != null ? MapToDto(negativeDefault) : null,
            neutralDefault != null ? MapToDto(neutralDefault) : null
        );
    }

    public async Task CreateDefaultTemplatesAsync(Guid businessId)
    {
        var existingTemplates = await _templateRepository.FindByBusinessIdAsync(businessId);
        if (existingTemplates.Any())
            return;

        var positiveTemplate = AutoResponseTemplate.CreateDefaultPositive(businessId);
        var negativeTemplate = AutoResponseTemplate.CreateDefaultNegative(businessId);
        var neutralTemplate = AutoResponseTemplate.CreateDefaultNeutral(businessId);

        await _templateRepository.AddAsync(positiveTemplate);
        await _templateRepository.AddAsync(negativeTemplate);
        await _templateRepository.AddAsync(neutralTemplate);
    }

    public async Task SetDefaultTemplateAsync(Guid templateId, Guid businessId)
    {
        var template = await _templateRepository.FindByIdAsync(templateId);
        if (template == null || template.BusinessId != businessId)
            throw new AutoResponseTemplateNotFoundException($"Template with ID {templateId} not found");

        var existingDefault = await _templateRepository.FindDefaultBySentimentAsync(businessId, template.Sentiment);
        if (existingDefault != null && existingDefault.Id != templateId)
        {
            existingDefault.IsDefault = false;
            await _templateRepository.UpdateAsync(existingDefault);
        }

        template.IsDefault = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _templateRepository.UpdateAsync(template);
    }

    public async Task<GeneratedAutoResponse?> GenerateResponseAsync(GenerateAutoResponseRequest request)
    {
        var settings = await _settingsRepository.FindBusinessSettingsByBusinessIdAsync(request.BusinessId);
        if (settings == null || !settings.AutoResponseEnabled)
            throw new AutoResponseDisabledException();

        var template = await _templateRepository.FindMatchingTemplateAsync(
            request.BusinessId,
            request.Sentiment,
            request.StarRating
        );

        if (template == null)
        {
            template = await _templateRepository.FindDefaultBySentimentAsync(request.BusinessId, request.Sentiment);
        }

        if (template == null)
            return null;

        var business = await _businessRepository.FindByIdAsync(request.BusinessId);
        var populatedContent = template.PopulateTemplate(
            request.ReviewerName,
            business?.Name ?? "Our Business",
            request.StarRating
        );

        await _templateRepository.IncrementUsageAsync(template.Id);

        return new GeneratedAutoResponse(
            template.Id,
            template.Name,
            populatedContent,
            false
        );
    }

    public async Task<TemplatePreviewResponse> PreviewTemplateAsync(PreviewTemplateRequest request)
    {
        var template = await _templateRepository.FindByIdAsync(request.TemplateId);
        if (template == null)
            throw new AutoResponseTemplateNotFoundException($"Template with ID {request.TemplateId} not found");

        var populatedContent = template.PopulateTemplate(
            request.ReviewerName,
            request.BusinessName,
            request.StarRating
        );

        return new TemplatePreviewResponse(
            template.Id,
            template.TemplateContent,
            populatedContent
        );
    }

    public async Task EnableAutoResponseAsync(Guid businessId, Guid enabledByUserId)
    {
        var settings = await _settingsRepository.FindBusinessSettingsByBusinessIdAsync(businessId);
        if (settings == null)
            throw new BusinessSettingsNotFoundException($"Settings not found for business {businessId}");

        settings.EnableAutoResponse();
        settings.ModifiedByUserId = enabledByUserId;
        await _settingsRepository.UpdateAsync(settings);

        await CreateDefaultTemplatesAsync(businessId);
    }

    public async Task DisableAutoResponseAsync(Guid businessId, Guid disabledByUserId)
    {
        var settings = await _settingsRepository.FindBusinessSettingsByBusinessIdAsync(businessId);
        if (settings == null)
            throw new BusinessSettingsNotFoundException($"Settings not found for business {businessId}");

        settings.DisableAutoResponse();
        settings.ModifiedByUserId = disabledByUserId;
        await _settingsRepository.UpdateAsync(settings);
    }

    private static AutoResponseTemplateDto MapToDto(AutoResponseTemplate template)
    {
        return new AutoResponseTemplateDto(
            template.Id,
            template.BusinessId,
            template.Name,
            template.Sentiment,
            template.Sentiment.ToString(),
            template.TemplateContent,
            template.IsActive,
            template.IsDefault,
            template.Priority,
            template.MinStarRating,
            template.MaxStarRating,
            template.TimesUsed,
            template.LastUsedAt,
            template.CreatedAt,
            template.UpdatedAt
        );
    }
}
