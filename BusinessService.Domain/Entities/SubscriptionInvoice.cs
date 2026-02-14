namespace BusinessService.Domain.Entities;

public class SubscriptionInvoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsAnnual { get; set; }
    public Guid BusinessId { get; set; }
    public string Platform { get; set; } = "paystack";
    public Guid SubscriptionId { get; set; }
    public string? PaymentUrl { get; set; }
    public string Email { get; set; } = default!;
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Status { get; set; } = "unpaid";
    public string? Payload { get; set; }
}
