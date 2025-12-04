using System.Net.Http.Json;
using BusinessService.Application.DTOs;
using BusinessService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessService.Application.Services;

public class BusinessSearchHttpProducer : IBusinessSearchProducer
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BusinessSearchHttpProducer> _logger;

    public BusinessSearchHttpProducer(HttpClient httpClient, ILogger<BusinessSearchHttpProducer> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task PublishBusinessCreatedAsync(BusinessDto dto)
    {
        await SendAsync("events/business-created", dto);
    }

    public async Task PublishBusinessUpdatedAsync(BusinessDto dto)
    {
        await SendAsync("events/business-updated", dto);
    }

    private async Task SendAsync<T>(string route, T payload)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(route, payload);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "SearchService returned {Status} while sending payload to {Route}",
                    response.StatusCode, route
                );
            }
            else
            {
                _logger.LogInformation(
                    "Successfully sent update to SearchService for route {Route}",
                    route
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed sending payload to SearchService route {Route}",
                route);
        }
    }
}