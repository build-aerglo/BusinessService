namespace BusinessService.Domain.Entities;

public class IdVerificationRequest
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string? IdVerificationNumber { get; set; }
    public string IdVerificationType { get; set; } = default!;
    public string? IdVerificationUrl { get; set; }
    public string? IdVerificationName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
