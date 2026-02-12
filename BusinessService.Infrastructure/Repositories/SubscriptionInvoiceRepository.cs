using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class SubscriptionInvoiceRepository : ISubscriptionInvoiceRepository
{
    private readonly DapperContext _context;

    public SubscriptionInvoiceRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionInvoice> AddAsync(SubscriptionInvoice invoice)
    {
        const string sql = """
                               INSERT INTO subscription_invoice (
                                   id, is_annual, business_id, platform, subscription_id,
                                   payment_url, email, reference, created_at, status
                               ) VALUES (
                                   @Id, @IsAnnual, @BusinessId, @Platform, @SubscriptionId,
                                   @PaymentUrl, @Email, @Reference, @CreatedAt, @Status
                               );
                           """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, invoice);
        return invoice;
    }

    public async Task<SubscriptionInvoice?> FindByIdAsync(Guid id)
    {
        const string sql = """
                               SELECT * FROM subscription_invoice WHERE id = @id;
                           """;

        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<SubscriptionInvoice>(sql, new { id });
    }
}
