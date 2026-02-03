using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class BusinessAutoResponseRepository : IBusinessAutoResponseRepository
{
    private readonly DapperContext _context;

    public BusinessAutoResponseRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<BusinessAutoResponse?> FindByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT
                business_id AS BusinessId,
                positive_response AS PositiveResponse,
                negative_response AS NegativeResponse,
                neutral_response AS NeutralResponse,
                allow_auto_response AS AllowAutoResponse
            FROM business_auto_response
            WHERE business_id = @businessId;
        """;

        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessAutoResponse>(sql, new { businessId });
    }

    public async Task UpdateAsync(BusinessAutoResponse autoResponse)
    {
        const string sql = """
            UPDATE business_auto_response
            SET positive_response = @PositiveResponse,
                negative_response = @NegativeResponse,
                neutral_response = @NeutralResponse,
                allow_auto_response = @AllowAutoResponse
            WHERE business_id = @BusinessId;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, autoResponse);
    }
}
