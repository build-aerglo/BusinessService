using System.ComponentModel.DataAnnotations;

namespace BusinessService.Application.DTOs;

public class UpdateBusinessRequest
{
    public string? Name { get; set; }
    public string? Website { get; set; }
    public string? BusinessAddress { get; set; }
    public string? Logo { get; set; }
    public Dictionary<string, string>? OpeningHours { get; set; }
    public string? BusinessEmail { get; set; }
    public string? BusinessPhoneNumber { get; set; }
    public string? CacNumber { get; set; }
    public string? AccessUsername { get; set; }
    public string? AccessNumber { get; set; }
    public Dictionary<string, string>? SocialMediaLinks { get; set; }
    public string? BusinessDescription { get; set; }

    // FIXED: Must be string[]?
    public string[]? Media { get; set; }

    public bool? IsVerified { get; set; }
    public string? ReviewLink { get; set; }
    public string? PreferredContactMethod { get; set; }

    public string[]? Highlights { get; set; }
    public string[]? Tags { get; set; }

    public string? AverageResponseTime { get; set; }
    public long? ProfileClicks { get; set; }

    // FIXED: Should use FaqRequestDto
    public List<FaqDto>? Faqs { get; set; }

    // FIXED: Nullable for PATCH, validated only if provided
    public List<Guid>? CategoryIds { get; set; }
    public string? BusinessStreet { get; set; }
    public string? BusinessCityTown { get; set; }
    public string? BusinessState{ get; set; }
}