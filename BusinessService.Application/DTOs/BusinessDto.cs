namespace BusinessService.Application.DTOs;

public record BusinessDto(
    Guid Id,
    string Name,
    string? Website,
    bool IsBranch,
    decimal AvgRating,
    long ReviewCount,
    Guid? ParentBusinessId,
    List<CategoryDto> Categories
);