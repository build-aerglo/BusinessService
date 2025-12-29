namespace BusinessService.Domain.Entities;

/// <summary>
/// External review source integration for businesses
/// Imports reviews from platforms like X, Instagram, Facebook, Chowdeck, Jumia, JiJi
/// </summary>
public class ExternalSource
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }

    // Source details
    public ExternalSourceType SourceType { get; set; }
    public string SourceName { get; set; } = default!;
    public string? SourceUrl { get; set; }
    public string? SourceAccountId { get; set; }

    // Connection status
    public ExternalSourceStatus Status { get; set; } = ExternalSourceStatus.Pending;
    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateTime? NextSyncAt { get; set; }
    public string? LastSyncError { get; set; }

    // Sync settings
    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncIntervalHours { get; set; } = 24;

    // Statistics
    public int TotalReviewsImported { get; set; }
    public int ReviewsImportedLastSync { get; set; }

    // Authentication (encrypted)
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ConnectedByUserId { get; set; }

    /// <summary>
    /// Marks the source as connected
    /// </summary>
    public void Connect(Guid userId, string? accessToken = null, string? refreshToken = null)
    {
        Status = ExternalSourceStatus.Connected;
        ConnectedAt = DateTime.UtcNow;
        ConnectedByUserId = userId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ScheduleNextSync();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disconnects the external source
    /// </summary>
    public void Disconnect()
    {
        Status = ExternalSourceStatus.Disconnected;
        AccessToken = null;
        RefreshToken = null;
        TokenExpiresAt = null;
        NextSyncAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a successful sync
    /// </summary>
    public void RecordSuccessfulSync(int reviewsImported)
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncError = null;
        TotalReviewsImported += reviewsImported;
        ReviewsImportedLastSync = reviewsImported;
        ScheduleNextSync();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed sync
    /// </summary>
    public void RecordFailedSync(string error)
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncError = error;
        ReviewsImportedLastSync = 0;
        Status = ExternalSourceStatus.Error;
        ScheduleNextSync();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Schedules the next sync based on interval
    /// </summary>
    private void ScheduleNextSync()
    {
        NextSyncAt = DateTime.UtcNow.AddHours(SyncIntervalHours);
    }

    /// <summary>
    /// Checks if sync is due
    /// </summary>
    public bool IsSyncDue => AutoSyncEnabled &&
                             Status == ExternalSourceStatus.Connected &&
                             NextSyncAt.HasValue &&
                             DateTime.UtcNow >= NextSyncAt.Value;
}

/// <summary>
/// Supported external source types
/// </summary>
public enum ExternalSourceType
{
    // Social Media
    Twitter = 0,
    Instagram = 1,
    Facebook = 2,

    // Marketplaces
    Chowdeck = 10,
    Jumia = 11,
    JiJi = 12,

    // Other
    GoogleMyBusiness = 20,
    TripAdvisor = 21,

    // Manual import
    CsvUpload = 100
}

/// <summary>
/// External source connection status
/// </summary>
public enum ExternalSourceStatus
{
    Pending = 0,
    Connected = 1,
    Disconnected = 2,
    Error = 3,
    RateLimited = 4,
    TokenExpired = 5
}
