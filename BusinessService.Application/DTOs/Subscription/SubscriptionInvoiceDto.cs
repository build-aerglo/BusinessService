namespace BusinessService.Application.DTOs.Subscription;

public class CheckoutRequest
{
    public string Email { get; set; } = default!;
    public Guid BusinessId { get; set; }
    public Guid SubscriptionId { get; set; }
    public bool IsAnnual { get; set; }
    public string? Platform { get; set; }
}

public record CheckoutResponse(
    string Message,
    Guid Invoice
);

public record SubscriptionInvoiceDto(
    Guid Id,
    bool IsAnnual,
    Guid BusinessId,
    string Platform,
    Guid SubscriptionId,
    string? PaymentUrl,
    string Email,
    string? Reference,
    DateTime CreatedAt,
    string? Status,
    SubscriptionPlanSummaryDto? Subscription
);

public record SubscriptionPlanSummaryDto(
    Guid Id,
    string Name,
    string? Tier,
    string? Description,
    decimal MonthlyPrice,
    decimal AnnualPrice
);
