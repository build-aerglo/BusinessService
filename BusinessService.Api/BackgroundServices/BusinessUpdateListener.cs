// BusinessService.Api/BackgroundServices/BusinessUpdateListener.cs

using BusinessService.Application.Interfaces;
using Npgsql;

namespace BusinessService.Api.BackgroundServices;

/// <summary>
/// Background service that listens for PostgreSQL NOTIFY events
/// when business ratings are updated, then re-indexes to Elasticsearch
/// </summary>
public class BusinessUpdateListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BusinessUpdateListener> _logger;
    private readonly IConfiguration _configuration;
    private NpgsqlConnection? _connection;

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

        try
        {
            // Get connection string from configuration
            var connectionString = _configuration.GetConnectionString("PostgresConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogError("PostgreSQL connection string not configured");
                return;
            }

            // Create persistent connection for LISTEN/NOTIFY
            _connection = new NpgsqlConnection(connectionString);
            await _connection.OpenAsync(stoppingToken);

            // Register notification handler
            _connection.Notification += async (sender, args) =>
            {
                try
                {
                    await OnBusinessUpdatedAsync(args.Payload, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling business update notification for {BusinessId}", args.Payload);
                }
            };

            // Start listening for notifications
            using var cmd = new NpgsqlCommand("LISTEN business_updated", _connection);
            await cmd.ExecuteNonQueryAsync(stoppingToken);

            _logger.LogInformation("✅ Now listening for business_updated notifications");

            // Keep connection alive and wait for notifications
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Wait() processes incoming notifications
                    await _connection.WaitAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Business Update Listener is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in notification wait loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Business Update Listener");
        }
        finally
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
            _logger.LogInformation("Business Update Listener stopped");
        }
    }

    private async Task OnBusinessUpdatedAsync(string businessIdString, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(businessIdString, out var businessId))
        {
            _logger.LogWarning("Invalid business ID in notification: {BusinessId}", businessIdString);
            return;
        }

        _logger.LogInformation("📢 Received update notification for business {BusinessId}", businessId);

        try
        {
            // Create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var businessService = scope.ServiceProvider.GetRequiredService<IBusinessService>();
            var searchProducer = scope.ServiceProvider.GetRequiredService<IBusinessSearchProducer>();

            // Fetch the updated business data
            var business = await businessService.GetBusinessAsync(businessId);

            // Publish to Elasticsearch via search service
            await searchProducer.PublishBusinessUpdatedAsync(business);

            _logger.LogInformation("✅ Successfully re-indexed business {BusinessId} to Elasticsearch", businessId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to re-index business {BusinessId} after rating update", businessId);
            // Don't throw - we don't want to crash the listener
        }
    }

    public override void Dispose()
    {
        _connection?.Dispose();
        base.Dispose();
    }
}