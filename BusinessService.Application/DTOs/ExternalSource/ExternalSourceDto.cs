using BusinessService.Domain.Entities;

namespace BusinessService.Application.DTOs.ExternalSource;

/// <summary>
/// External source DTO
/// </summary>
public record ExternalSourceDto(
    Guid Id,
    Guid BusinessId,
    ExternalSourceType SourceType,
    string SourceTypeName,
    string SourceName,
    string? SourceUrl,
    ExternalSourceStatus Status,
    string StatusName,
    DateTime? ConnectedAt,
    DateTime? LastSyncAt,
    DateTime? NextSyncAt,
    string? LastSyncError,
    bool AutoSyncEnabled,
    int SyncIntervalHours,
    int TotalReviewsImported,
    int ReviewsImportedLastSync,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Request to connect an external source
/// </summary>
public class ConnectExternalSourceRequest
{
    public Guid BusinessId { get; set; }
    public ExternalSourceType SourceType { get; set; }
    public string SourceName { get; set; } = default!;
    public string? SourceUrl { get; set; }
    public string? SourceAccountId { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}

/// <summary>
/// Request to disconnect an external source
/// </summary>
public class DisconnectExternalSourceRequest
{
    public Guid SourceId { get; set; }
}

/// <summary>
/// Request to trigger manual sync
/// </summary>
public class TriggerSyncRequest
{
    public Guid SourceId { get; set; }
}

/// <summary>
/// Request to update sync settings
/// </summary>
public class UpdateSyncSettingsRequest
{
    public Guid SourceId { get; set; }
    public bool? AutoSyncEnabled { get; set; }
    public int? SyncIntervalHours { get; set; }
}

/// <summary>
/// External sources list response
/// </summary>
public record ExternalSourcesListResponse(
    Guid BusinessId,
    int ConnectedCount,
    int MaxAllowed,
    int RemainingSlots,
    List<ExternalSourceDto> Sources
);

/// <summary>
/// Available external source types
/// </summary>
public record AvailableSourceTypeDto(
    ExternalSourceType Type,
    string Name,
    string Description,
    string IconUrl,
    bool RequiresAuth,
    bool IsAvailable
);

/// <summary>
/// Sync result response
/// </summary>
public record SyncResultResponse(
    Guid SourceId,
    bool Success,
    int ReviewsImported,
    string? ErrorMessage,
    DateTime SyncedAt,
    DateTime? NextSyncAt
);

/// <summary>
/// CSV upload request for manual import
/// </summary>
public class CsvUploadRequest
{
    public Guid BusinessId { get; set; }
    public string FileName { get; set; } = default!;
    public string FileContentBase64 { get; set; } = default!;
}

/// <summary>
/// CSV upload result
/// </summary>
public record CsvUploadResult(
    int TotalRows,
    int ImportedRows,
    int SkippedRows,
    List<string> Errors
);
