using BusinessService.Application.DTOs.Subscription;

namespace BusinessService.Application.Interfaces;

public interface ISubscriptionInvoiceService
{
    Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request);
    Task<SubscriptionInvoiceDto?> GetInvoiceAsync(Guid invoiceId);
    Task<PaymentVerificationResult> ConfirmInvoiceAsync(string reference);
}
