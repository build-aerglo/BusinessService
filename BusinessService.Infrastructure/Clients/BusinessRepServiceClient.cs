using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using BusinessService.Application.Interfaces;

namespace BusinessService.Infrastructure.Clients;

/// <summary>
/// Client for communicating with UserService to get business rep information
/// </summary>
public class BusinessRepServiceClient : IBusinessRepServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BusinessRepServiceClient> _logger;

    public BusinessRepServiceClient(HttpClient httpClient, ILogger<BusinessRepServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BusinessRepDto?> GetParentRepByBusinessIdAsync(Guid businessId)
    {
        var url = $"/api/User/business-rep/parent/{businessId}";
        _logger.LogInformation("Sending GET request to: {Url}", $"{_httpClient.BaseAddress}{url}");

        try
        {
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Parent business rep not found for business {BusinessId}", businessId);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BusinessRepDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error getting parent rep for business {BusinessId}", businessId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting parent rep for business {BusinessId}", businessId);
            return null;
        }
    }

    public async Task<BusinessRepDto?> GetBusinessRepByIdAsync(Guid businessRepId)
    {
        var url = $"/api/User/business-rep/{businessRepId}";
        _logger.LogInformation("Sending GET request to: {Url}", $"{_httpClient.BaseAddress}{url}");

        try
        {
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Business rep {BusinessRepId} not found", businessRepId);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BusinessRepDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error getting business rep {BusinessRepId}", businessRepId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting business rep {BusinessRepId}", businessRepId);
            return null;
        }
    }
}
