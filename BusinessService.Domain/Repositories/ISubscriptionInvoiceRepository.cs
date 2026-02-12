using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface ISubscriptionInvoiceRepository
{
    Task<SubscriptionInvoice> AddAsync(SubscriptionInvoice invoice);
    Task<SubscriptionInvoice?> FindByIdAsync(Guid id);
}
