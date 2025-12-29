using BusinessService.Application.DTOs.ExternalSource;
using BusinessService.Domain.Entities;

namespace BusinessService.Application.Interfaces;

public interface IExternalSourceService
{
    // Source management
    Task<ExternalSourcesListResponse> GetExternalSourcesAsync(Guid businessId);
    Task<ExternalSourceDto?> GetExternalSourceByIdAsync(Guid sourceId);
    Task<ExternalSourceDto> ConnectSourceAsync(ConnectExternalSourceRequest request, Guid connectedByUserId);
    Task DisconnectSourceAsync(Guid sourceId);

    // Sync management
    Task<SyncResultResponse> TriggerSyncAsync(Guid sourceId);
    Task<ExternalSourceDto> UpdateSyncSettingsAsync(UpdateSyncSettingsRequest request);
    Task ProcessDueSyncsAsync();

    // CSV import
    Task<CsvUploadResult> ImportFromCsvAsync(CsvUploadRequest request);

    // Available sources
    Task<List<AvailableSourceTypeDto>> GetAvailableSourceTypesAsync();
    Task<bool> CanConnectMoreSourcesAsync(Guid businessId);
}
