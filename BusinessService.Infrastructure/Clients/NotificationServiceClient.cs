using System.Net.Http.Json;
using BusinessService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessService.Infrastructure.Clients;

public class NotificationServiceClient : INotificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationServiceClient> _logger;

    public NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendNotificationAsync(NotificationRequest request)
    {
        _logger.LogInformation("Sending {Template} notification via {Channel} to {Recipient}",
            request.Template, request.Channel, request.Recipient);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/notification", request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Notification service returned {StatusCode} for {Template} to {Recipient}. Response: {Body}",
                    response.StatusCode, request.Template, request.Recipient, body);
            }
            else
            {
                _logger.LogInformation("Notification sent successfully for {Template} to {Recipient}", request.Template, request.Recipient);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error sending notification to {Recipient}", request.Recipient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending notification to {Recipient}", request.Recipient);
        }
    }
}
