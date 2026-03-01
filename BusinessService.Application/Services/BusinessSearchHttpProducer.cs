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
        await SendAsync("events/business-created", MapToPayload(dto));
    }

    public async Task PublishBusinessUpdatedAsync(BusinessDto dto)
    {
        await SendAsync("events/business-updated", MapToPayload(dto));
    }

    /// <summary>
    /// Maps BusinessDto to a payload that matches BusinessUpdatedEvent / BusinessCreatedEvent
    /// on the ReviewService side. The key fix is Tags: BusinessDto carries List&lt;TagDto&gt;
    /// (objects with Id, CategoryId, Name) but ReviewService expects string[] of tag names.
    /// OpeningHours is also widened from Dictionary&lt;string,string&gt; to Dictionary&lt;string,object&gt;
    /// to match the receiving type exactly.
    /// </summary>
    private static object MapToPayload(BusinessDto dto) => new
    {
        dto.Id,
        dto.Name,
        dto.Website,
        dto.IsBranch,
        dto.AvgRating,
        dto.ReviewCount,
        dto.ParentBusinessId,
        dto.Categories,
        dto.BusinessAddress,
        dto.Logo,
        // Widen to Dictionary<string, object> to match BusinessUpdatedEvent
        OpeningHours = dto.OpeningHours?
            .ToDictionary(k => k.Key, v => (object)v.Value),
        dto.BusinessEmail,
        dto.BusinessPhoneNumber,
        dto.CacNumber,
        dto.AccessUsername,
        dto.AccessNumber,
        dto.SocialMediaLinks,
        dto.BusinessDescription,
        dto.Media,
        dto.IsVerified,
        dto.ReviewLink,
        dto.PreferredContactMethod,
        dto.Highlights,
        // KEY FIX: extract Name strings from List<TagDto> → string[]
        // so ReviewService can bind into BusinessUpdatedEvent.Tags (string[])
        Tags = dto.Tags?
            .Select(t => t.Name)
            .ToArray(),
        dto.AverageResponseTime,
        dto.ProfileClicks,
        dto.Faqs,
        dto.QrCodeBase64
    };

    private async Task SendAsync<T>(string route, T payload)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(route, payload);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "SearchService returned {Status} for route {Route}",
                    response.StatusCode, route);
            }
            else
            {
                _logger.LogInformation(
                    "Successfully sent update to SearchService for route {Route}",
                    route);
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