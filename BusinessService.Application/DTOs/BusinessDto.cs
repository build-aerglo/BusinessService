namespace BusinessService.Application.DTOs;

public record BusinessDto(
    Guid Id,
    string Name,
    string? Website,
    bool IsBranch,
    decimal AvgRating,
    long ReviewCount,
    Guid? ParentBusinessId,
    List<CategoryDto> Categories,
    string? BusinessAddress,
    string? Logo,
    Dictionary<string, object>? OpeningHours,
    string? BusinessEmail,
    string? BusinessPhoneNumber,
    string? CacNumber,
    string? AccessUsername,
    string? AccessNumber,
    Dictionary<string, string>? SocialMediaLinks,
    string? BusinessDescription,
    string[]? Media,
    bool IsVerified,
    string? ReviewLink,
    string? PreferredContactMethod
);