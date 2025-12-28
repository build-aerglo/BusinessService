namespace BusinessService.Domain.Entities;

public class BusinessClaims
{
    public Guid Id { get; set; }
    public string Role { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Email { get; set; } = default!;
    public string? Phone { get; set; } = default!;
}