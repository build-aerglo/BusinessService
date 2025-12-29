using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class BusinessUserRepository : IBusinessUserRepository
{
    private readonly DapperContext _context;

    public BusinessUserRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<BusinessUser?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM business_user WHERE id = @id;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessUser>(sql, new { id });
    }

    public async Task<BusinessUser?> FindByEmailAndBusinessIdAsync(string email, Guid businessId)
    {
        const string sql = "SELECT * FROM business_user WHERE LOWER(email) = LOWER(@email) AND business_id = @businessId;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessUser>(sql, new { email, businessId });
    }

    public async Task<BusinessUser?> FindByInvitationTokenAsync(string token)
    {
        const string sql = "SELECT * FROM business_user WHERE invitation_token = @token;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessUser>(sql, new { token });
    }

    public async Task<List<BusinessUser>> FindByBusinessIdAsync(Guid businessId)
    {
        const string sql = "SELECT * FROM business_user WHERE business_id = @businessId ORDER BY is_owner DESC, created_at;";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessUser>(sql, new { businessId });
        return results.ToList();
    }

    public async Task<List<BusinessUser>> FindActiveByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT * FROM business_user
            WHERE business_id = @businessId
            AND status = @status
            AND is_enabled = true
            ORDER BY is_owner DESC, created_at;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessUser>(sql, new
        {
            businessId,
            status = (int)BusinessUserStatus.Active
        });
        return results.ToList();
    }

    public async Task<BusinessUser?> FindOwnerByBusinessIdAsync(Guid businessId)
    {
        const string sql = "SELECT * FROM business_user WHERE business_id = @businessId AND is_owner = true LIMIT 1;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessUser>(sql, new { businessId });
    }

    public async Task AddAsync(BusinessUser user)
    {
        const string sql = """
            INSERT INTO business_user (
                id, business_id, user_id, email, name, phone_number,
                role, is_owner, status, invited_at, accepted_at,
                invitation_token, invitation_expires_at, is_enabled,
                enabled_by_user_id, disabled_at, created_at, updated_at, created_by_user_id
            ) VALUES (
                @Id, @BusinessId, @UserId, @Email, @Name, @PhoneNumber,
                @Role, @IsOwner, @Status, @InvitedAt, @AcceptedAt,
                @InvitationToken, @InvitationExpiresAt, @IsEnabled,
                @EnabledByUserId, @DisabledAt, @CreatedAt, @UpdatedAt, @CreatedByUserId
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, user);
    }

    public async Task UpdateAsync(BusinessUser user)
    {
        const string sql = """
            UPDATE business_user SET
                user_id = @UserId, email = @Email, name = @Name, phone_number = @PhoneNumber,
                role = @Role, is_owner = @IsOwner, status = @Status, invited_at = @InvitedAt,
                accepted_at = @AcceptedAt, invitation_token = @InvitationToken,
                invitation_expires_at = @InvitationExpiresAt, is_enabled = @IsEnabled,
                enabled_by_user_id = @EnabledByUserId, disabled_at = @DisabledAt, updated_at = @UpdatedAt
            WHERE id = @Id;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, user);
    }

    public async Task<int> CountActiveByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT COUNT(*) FROM business_user
            WHERE business_id = @businessId
            AND status = @status
            AND is_enabled = true;
        """;
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            businessId,
            status = (int)BusinessUserStatus.Active
        });
    }

    public async Task<bool> ExistsByEmailAndBusinessIdAsync(string email, Guid businessId)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM business_user WHERE LOWER(email) = LOWER(@email) AND business_id = @businessId);";
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { email, businessId });
    }
}
