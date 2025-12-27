namespace BusinessService.Application.DTOs;

public record BusinessClaimsDto(
    Guid Id,
    string Name,
    string Role,
    string? Email,
    string? Phone);