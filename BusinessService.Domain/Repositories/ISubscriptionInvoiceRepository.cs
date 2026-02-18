using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface ISubscriptionInvoiceRepository
{
    Task<SubscriptionInvoice> AddAsync(SubscriptionInvoice invoice);
    Task<SubscriptionInvoice?> FindByIdAsync(Guid id);
    Task UpdateStatusAsync(Guid invoiceId, string status, string? errorMessage = null);
    Task<SubscriptionInvoiceWithError?> FindByReferenceAsync(string reference);
}
