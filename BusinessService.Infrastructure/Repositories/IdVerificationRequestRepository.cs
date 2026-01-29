using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class IdVerificationRequestRepository : IIdVerificationRequestRepository
{
    private readonly DapperContext _context;

    public IdVerificationRequestRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IdVerificationRequest?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM id_verification_request WHERE id = @id;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<IdVerificationRequest>(sql, new { id });
    }

    public async Task<IdVerificationRequest?> FindByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT * FROM id_verification_request
            WHERE business_id = @businessId
            ORDER BY created_at DESC
            LIMIT 1;
        """;
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<IdVerificationRequest>(sql, new { businessId });
    }

    public async Task<List<IdVerificationRequest>> FindAllByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT * FROM id_verification_request
            WHERE business_id = @businessId
            ORDER BY created_at DESC;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<IdVerificationRequest>(sql, new { businessId });
        return results.ToList();
    }

    public async Task AddAsync(IdVerificationRequest request)
    {
        const string sql = """
            INSERT INTO id_verification_request (
                id, business_id, id_verification_number, id_verification_type,
                id_verification_url, id_verification_name, created_at
            ) VALUES (
                @Id, @BusinessId, @IdVerificationNumber, @IdVerificationType,
                @IdVerificationUrl, @IdVerificationName, @CreatedAt
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, request);
    }
}
