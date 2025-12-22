namespace BusinessService.Application.DTOs;

public record BusinessSummaryDto(
    Guid Id,
    string Name,
    decimal AvgRating,
    long ReviewCount,
    bool IsBranch,
    Guid? ParentBusinessId
);
