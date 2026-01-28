namespace BusinessService.Application.DTOs;

public record BusinessSummaryResponseDto(
    Guid Id,
    string Name,
    decimal AvgRating,
    long ReviewCount,
    bool IsBranch,
    List<CategoryDto>? Categories,
    string? BusinessAddress,
    string? Logo,
    string? BusinessPhoneNumber,
    List<TagDto>? Tags,
    string? ReviewSummary,
    bool? IsVerified,
    string? BusinessStreet,
    string? BusinessCityTown,
    string? BusinessState,
    bool IdVerified,
    string? IdVerificationType
    );