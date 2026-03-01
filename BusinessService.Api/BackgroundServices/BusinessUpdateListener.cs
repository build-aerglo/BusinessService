// BusinessService.Api/BackgroundServices/BusinessUpdateListener.cs

using BusinessService.Application.Interfaces;
using Npgsql;

namespace BusinessService.Api.BackgroundServices;

/// <summary>
/// Background service that listens for PostgreSQL NOTIFY events
/// when business ratings are updated, then re-indexes to Elasticsearch.
/// Includes full reconnection logic so a dropped DB connection does not
/// permanently kill the listener.
/// </summary>
public class BusinessUpdateListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BusinessUpdateListener> _logger;
    private readonly IConfiguration _configuration;

    // Reconnection back-off: 5s → 10s → 20s → … capped at 60s
    private static readonly TimeSpan MinDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(60);

    public BusinessUpdateListener(
        IServiceProvider serviceProvider,
        ILogger<BusinessUpdateListener> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Business Update Listener started - waiting for rating updates...");

        var connectionString = _configuration.GetConnectionString("PostgresConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError("PostgreSQL connection string not configured. BusinessUpdateListener will not start.");
            return;
        }

        var delay = MinDelay;

        // Outer loop: reconnect whenever the connection is lost
        while (!stoppingToken.IsCancellationRequested)
        {
            NpgsqlConnection? connection = null;
            try
            {
                connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(stoppingToken);

                // Reset back-off on a successful connection
                delay = MinDelay;

                connection.Notification += async (_, args) =>
                {
                    try
                    {
                        await OnBusinessUpdatedAsync(args.Payload, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error handling business update notification for payload '{Payload}'",
                            args.Payload);
                    }
                };

                using (var cmd = new NpgsqlCommand("LISTEN business_updated", connection))
                {
                    await cmd.ExecuteNonQueryAsync(stoppingToken);
                }

                _logger.LogInformation("✅ Now listening for business_updated notifications");

                // Inner loop: process notifications until the connection dies
                while (!stoppingToken.IsCancellationRequested)
                {
                    // WaitAsync blocks until a notification arrives or the connection drops
                    await connection.WaitAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Clean shutdown — exit both loops
                _logger.LogInformation("Business Update Listener is stopping gracefully.");
                break;
            }
            catch (Exception ex)
            {
                // Connection lost or failed to open — log and reconnect after back-off
                _logger.LogError(ex,
                    "Business Update Listener connection lost. Reconnecting in {Delay}s...",
                    delay.TotalSeconds);
            }
            finally
            {
                if (connection is not null)
                {
                    try { await connection.CloseAsync(); } catch { /* ignore */ }
                    await connection.DisposeAsync();
                }
            }

            if (stoppingToken.IsCancellationRequested) break;

            // Wait before reconnecting, then increase back-off (cap at MaxDelay)
            await Task.Delay(delay, stoppingToken).ContinueWith(_ => { }); // swallow cancellation
            delay = delay * 2 < MaxDelay ? delay * 2 : MaxDelay;
        }

        _logger.LogInformation("Business Update Listener stopped.");
    }

    private async Task OnBusinessUpdatedAsync(string businessIdString, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(businessIdString, out var businessId))
        {
            _logger.LogWarning("Invalid business ID in notification: '{BusinessId}'", businessIdString);
            return;
        }

        _logger.LogInformation("📢 Received update notification for business {BusinessId}", businessId);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var businessService = scope.ServiceProvider.GetRequiredService<IBusinessService>();
            var searchProducer = scope.ServiceProvider.GetRequiredService<IBusinessSearchProducer>();

            var business = await businessService.GetBusinessAsync(businessId);
            await searchProducer.PublishBusinessUpdatedAsync(business);

            _logger.LogInformation("✅ Successfully re-indexed business {BusinessId} to Elasticsearch", businessId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to re-index business {BusinessId} after rating update", businessId);
            // Don't rethrow — one failed notification must not kill the listener
        }
    }
}