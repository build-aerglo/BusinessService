using BusinessService.Application.DTOs.ExternalSource;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;

namespace BusinessService.Application.Services;

public class ExternalSourceService : IExternalSourceService
{
    private readonly IExternalSourceRepository _sourceRepository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IBusinessSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;

    public ExternalSourceService(
        IExternalSourceRepository sourceRepository,
        IBusinessRepository businessRepository,
        IBusinessSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository)
    {
        _sourceRepository = sourceRepository;
        _businessRepository = businessRepository;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
    }

    public async Task<ExternalSourcesListResponse> GetExternalSourcesAsync(Guid businessId)
    {
        var sources = await _sourceRepository.FindByBusinessIdAsync(businessId);
        var connectedCount = sources.Count(s => s.Status == ExternalSourceStatus.Connected);

        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(businessId);
        var plan = subscription != null
            ? await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId)
            : await _planRepository.FindByTierAsync(SubscriptionTier.Basic);

        var maxAllowed = plan?.ExternalSourceLimit ?? 1;

        return new ExternalSourcesListResponse(
            businessId,
            connectedCount,
            maxAllowed,
            Math.Max(0, maxAllowed - connectedCount),
            sources.Select(MapToDto).ToList()
        );
    }

    public async Task<ExternalSourceDto?> GetExternalSourceByIdAsync(Guid sourceId)
    {
        var source = await _sourceRepository.FindByIdAsync(sourceId);
        return source != null ? MapToDto(source) : null;
    }

    public async Task<ExternalSourceDto> ConnectSourceAsync(ConnectExternalSourceRequest request, Guid connectedByUserId)
    {
        var business = await _businessRepository.FindByIdAsync(request.BusinessId);
        if (business == null)
            throw new BusinessNotFoundException($"Business with ID {request.BusinessId} not found");

        if (await _sourceRepository.ExistsByBusinessIdAndTypeAsync(request.BusinessId, request.SourceType))
            throw new ExternalSourceAlreadyConnectedException($"Source type {request.SourceType} is already connected");

        if (!await CanConnectMoreSourcesAsync(request.BusinessId))
        {
            var connectedCount = await _sourceRepository.CountConnectedByBusinessIdAsync(request.BusinessId);
            var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(request.BusinessId);
            var plan = subscription != null
                ? await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId)
                : await _planRepository.FindByTierAsync(SubscriptionTier.Basic);
            throw new ExternalSourceLimitExceededException(plan?.ExternalSourceLimit ?? 1, connectedCount);
        }

        var source = new ExternalSource
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            SourceType = request.SourceType,
            SourceName = request.SourceName,
            SourceUrl = request.SourceUrl,
            SourceAccountId = request.SourceAccountId,
            Status = ExternalSourceStatus.Pending,
            AutoSyncEnabled = true,
            SyncIntervalHours = 24,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        source.Connect(connectedByUserId, request.AccessToken, request.RefreshToken);
        await _sourceRepository.AddAsync(source);

        return MapToDto(source);
    }

    public async Task DisconnectSourceAsync(Guid sourceId)
    {
        var source = await _sourceRepository.FindByIdAsync(sourceId);
        if (source == null)
            throw new ExternalSourceNotFoundException($"Source with ID {sourceId} not found");

        source.Disconnect();
        await _sourceRepository.UpdateAsync(source);
    }

    public async Task<SyncResultResponse> TriggerSyncAsync(Guid sourceId)
    {
        var source = await _sourceRepository.FindByIdAsync(sourceId);
        if (source == null)
            throw new ExternalSourceNotFoundException($"Source with ID {sourceId} not found");

        if (source.Status != ExternalSourceStatus.Connected)
            throw new ExternalSourceSyncException("Source is not connected");

        try
        {
            // In a real implementation, this would call the external source API
            // For now, we'll simulate a successful sync
            var reviewsImported = 0; // Placeholder
            source.RecordSuccessfulSync(reviewsImported);
            await _sourceRepository.UpdateAsync(source);

            return new SyncResultResponse(
                sourceId,
                true,
                reviewsImported,
                null,
                DateTime.UtcNow,
                source.NextSyncAt
            );
        }
        catch (Exception ex)
        {
            source.RecordFailedSync(ex.Message);
            await _sourceRepository.UpdateAsync(source);

            return new SyncResultResponse(
                sourceId,
                false,
                0,
                ex.Message,
                DateTime.UtcNow,
                source.NextSyncAt
            );
        }
    }

    public async Task<ExternalSourceDto> UpdateSyncSettingsAsync(UpdateSyncSettingsRequest request)
    {
        var source = await _sourceRepository.FindByIdAsync(request.SourceId);
        if (source == null)
            throw new ExternalSourceNotFoundException($"Source with ID {request.SourceId} not found");

        if (request.AutoSyncEnabled.HasValue)
            source.AutoSyncEnabled = request.AutoSyncEnabled.Value;

        if (request.SyncIntervalHours.HasValue)
            source.SyncIntervalHours = request.SyncIntervalHours.Value;

        source.UpdatedAt = DateTime.UtcNow;
        await _sourceRepository.UpdateAsync(source);

        return MapToDto(source);
    }

    public async Task ProcessDueSyncsAsync()
    {
        var dueSources = await _sourceRepository.FindDueSyncAsync();

        foreach (var source in dueSources)
        {
            await TriggerSyncAsync(source.Id);
        }
    }

    public async Task<CsvUploadResult> ImportFromCsvAsync(CsvUploadRequest request)
    {
        var business = await _businessRepository.FindByIdAsync(request.BusinessId);
        if (business == null)
            throw new BusinessNotFoundException($"Business with ID {request.BusinessId} not found");

        // In a real implementation, this would parse the CSV and import reviews
        // For now, we'll return a placeholder result
        return new CsvUploadResult(0, 0, 0, new List<string>());
    }

    public async Task<List<AvailableSourceTypeDto>> GetAvailableSourceTypesAsync()
    {
        return new List<AvailableSourceTypeDto>
        {
            new(ExternalSourceType.Twitter, "X (Twitter)", "Import reviews from X posts", "twitter-icon", true, true),
            new(ExternalSourceType.Instagram, "Instagram", "Import reviews from Instagram comments", "instagram-icon", true, true),
            new(ExternalSourceType.Facebook, "Facebook", "Import reviews from Facebook page", "facebook-icon", true, true),
            new(ExternalSourceType.Chowdeck, "Chowdeck", "Import reviews from Chowdeck orders", "chowdeck-icon", true, true),
            new(ExternalSourceType.Jumia, "Jumia", "Import reviews from Jumia products", "jumia-icon", true, true),
            new(ExternalSourceType.JiJi, "JiJi", "Import reviews from JiJi listings", "jiji-icon", true, true),
            new(ExternalSourceType.GoogleMyBusiness, "Google My Business", "Import reviews from Google", "google-icon", true, true),
            new(ExternalSourceType.CsvUpload, "CSV Upload", "Manually upload reviews from CSV file", "csv-icon", false, true)
        };
    }

    public async Task<bool> CanConnectMoreSourcesAsync(Guid businessId)
    {
        var connectedCount = await _sourceRepository.CountConnectedByBusinessIdAsync(businessId);
        var subscription = await _subscriptionRepository.FindActiveByBusinessIdAsync(businessId);
        var plan = subscription != null
            ? await _planRepository.FindByIdAsync(subscription.SubscriptionPlanId)
            : await _planRepository.FindByTierAsync(SubscriptionTier.Basic);

        var maxAllowed = plan?.ExternalSourceLimit ?? 1;
        return maxAllowed == int.MaxValue || connectedCount < maxAllowed;
    }

    private static ExternalSourceDto MapToDto(ExternalSource source)
    {
        return new ExternalSourceDto(
            source.Id,
            source.BusinessId,
            source.SourceType,
            source.SourceType.ToString(),
            source.SourceName,
            source.SourceUrl,
            source.Status,
            source.Status.ToString(),
            source.ConnectedAt,
            source.LastSyncAt,
            source.NextSyncAt,
            source.LastSyncError,
            source.AutoSyncEnabled,
            source.SyncIntervalHours,
            source.TotalReviewsImported,
            source.ReviewsImportedLastSync,
            source.CreatedAt,
            source.UpdatedAt
        );
    }
}
