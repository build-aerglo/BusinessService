namespace BusinessService.Domain.Entities;

public class BusinessBranches{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = default!;
    public string? BranchStreet { get; set; } = default!;
    public string? BranchCityTown { get; set; }
    public string? BranchState { get; set; }
    public string? BranchStatus { get; set; }
}