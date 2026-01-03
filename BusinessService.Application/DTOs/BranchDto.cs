namespace BusinessService.Application.DTOs;

public record BranchDto(
    Guid BusinessId,
    string Name,
    string? BranchStreet,
    string? BranchCityTown,
    string? BranchState
    );
    
    public record BranchUpdateDto(
        Guid Id,
        Guid BusinessId,
        string Name,
        string? BranchStreet,
        string? BranchCityTown,
        string? BranchState
    );