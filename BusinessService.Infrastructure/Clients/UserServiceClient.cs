using System.Net;
using System.Net.Http.Json;
using BusinessService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessService.Infrastructure.Clients;

/// <summary>
/// Client for communicating with UserService
/// </summary>
public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(HttpClient httpClient, ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> IsSupportUserAsync(Guid userId)
    {
        var url = $"/api/User/support-user/{userId}/exists";
        _logger.LogInformation("Checking if user {UserId} is a support user", userId);

        try
        {
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("User {UserId} is not a support user", userId);
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to check support user status for {UserId}: {StatusCode}", 
                    userId, response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<SupportUserCheckResponse>();
            return result?.IsSupportUser ?? false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error checking support user status for {UserId}", userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking support user status for {UserId}", userId);
            return false;
        }
    }
}

/// <summary>
/// Response DTO for support user check
/// </summary>
internal record SupportUserCheckResponse(bool IsSupportUser);