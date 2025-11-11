using BusinessService.Application.Interfaces;

namespace BusinessService.Api.BackgroundServices;

/// <summary>
/// Background service that periodically checks and disables expired DnD modes
/// </summary>
public class DndModeExpiryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DndModeExpiryBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15); // Check every 15 minutes

    public DndModeExpiryBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DndModeExpiryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DnD Mode Expiry Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredDndModesAsync();
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DnD Mode Expiry Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing expired DnD modes");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("DnD Mode Expiry Background Service stopped");
    }

    private async Task ProcessExpiredDndModesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<IBusinessSettingsService>();

        _logger.LogDebug("Checking for expired DnD modes...");

        try
        {
            await settingsService.ProcessExpiredDndModesAsync();
            _logger.LogDebug("Expired DnD modes processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process expired DnD modes");
            throw;
        }
    }
}