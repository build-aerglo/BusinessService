namespace BusinessService.Application.DTOs;

public record BranchDto(
    Guid BusinessId,
    string BranchName,
    string? BranchStreet,
    string? BranchCityTown,
    string? BranchState
    );
    
    public record BranchUpdateDto(
        Guid Id,
        Guid BusinessId,
        string BranchName,
        string? BranchStreet,
        string? BranchCityTown,
        string? BranchState
    );