namespace BusinessService.Application.Interfaces;

public interface IPaymentInitiator
{
    Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request);
}

public class PaymentInitiationRequest
{
    public string Email { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? CallbackUrl { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public record PaymentInitiationResult(
    bool Success,
    string? Reference,
    string? PaymentUrl,
    string? Error
);
