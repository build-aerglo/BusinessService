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
    string[]? Tags,
    string? ReviewSummary,
    bool? isVerified
    );