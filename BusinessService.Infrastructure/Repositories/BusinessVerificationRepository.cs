using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class BusinessVerificationRepository : IBusinessVerificationRepository
{
    private readonly DapperContext _context;

    public BusinessVerificationRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<BusinessVerification?> FindByBusinessIdAsync(Guid businessId)
    {
        const string sql = "SELECT * FROM business_verification WHERE business_id = @businessId;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessVerification>(sql, new { businessId });
    }

    public async Task<BusinessVerification?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM business_verification WHERE id = @id;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessVerification>(sql, new { id });
    }

    public async Task AddAsync(BusinessVerification verification)
    {
        const string sql = """
            INSERT INTO business_verification (
                id, business_id, cac_verified, cac_number, cac_verified_at,
                phone_verified, phone_number, phone_verified_at,
                email_verified, email, email_verified_at,
                address_verified, address_proof_url, address_verified_at,
                online_presence_verified, website_url, social_media_url, online_presence_verified_at,
                other_ids_verified, tin_number, license_number, other_id_document_url, other_ids_verified_at,
                business_domain_email_verified, business_domain_email, business_domain_email_verified_at,
                requires_reverification, reverification_reason, reverification_requested_at,
                created_at, updated_at, verified_by_user_id
            ) VALUES (
                @Id, @BusinessId, @CacVerified, @CacNumber, @CacVerifiedAt,
                @PhoneVerified, @PhoneNumber, @PhoneVerifiedAt,
                @EmailVerified, @Email, @EmailVerifiedAt,
                @AddressVerified, @AddressProofUrl, @AddressVerifiedAt,
                @OnlinePresenceVerified, @WebsiteUrl, @SocialMediaUrl, @OnlinePresenceVerifiedAt,
                @OtherIdsVerified, @TinNumber, @LicenseNumber, @OtherIdDocumentUrl, @OtherIdsVerifiedAt,
                @BusinessDomainEmailVerified, @BusinessDomainEmail, @BusinessDomainEmailVerifiedAt,
                @RequiresReverification, @ReverificationReason, @ReverificationRequestedAt,
                @CreatedAt, @UpdatedAt, @VerifiedByUserId
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, verification);
    }

    public async Task UpdateAsync(BusinessVerification verification)
    {
        const string sql = """
            UPDATE business_verification SET
                cac_verified = @CacVerified, cac_number = @CacNumber, cac_verified_at = @CacVerifiedAt,
                phone_verified = @PhoneVerified, phone_number = @PhoneNumber, phone_verified_at = @PhoneVerifiedAt,
                email_verified = @EmailVerified, email = @Email, email_verified_at = @EmailVerifiedAt,
                address_verified = @AddressVerified, address_proof_url = @AddressProofUrl, address_verified_at = @AddressVerifiedAt,
                online_presence_verified = @OnlinePresenceVerified, website_url = @WebsiteUrl,
                social_media_url = @SocialMediaUrl, online_presence_verified_at = @OnlinePresenceVerifiedAt,
                other_ids_verified = @OtherIdsVerified, tin_number = @TinNumber,
                license_number = @LicenseNumber, other_id_document_url = @OtherIdDocumentUrl, other_ids_verified_at = @OtherIdsVerifiedAt,
                business_domain_email_verified = @BusinessDomainEmailVerified,
                business_domain_email = @BusinessDomainEmail, business_domain_email_verified_at = @BusinessDomainEmailVerifiedAt,
                requires_reverification = @RequiresReverification, reverification_reason = @ReverificationReason,
                reverification_requested_at = @ReverificationRequestedAt,
                updated_at = @UpdatedAt, verified_by_user_id = @VerifiedByUserId
            WHERE id = @Id;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, verification);
    }

    public async Task<bool> ExistsByBusinessIdAsync(Guid businessId)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM business_verification WHERE business_id = @businessId);";
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { businessId });
    }

    public async Task<List<BusinessVerification>> FindByVerificationLevelAsync(VerificationLevel level)
    {
        // This requires computing the level in the application layer
        // as it's calculated based on multiple boolean fields
        const string sql = "SELECT * FROM business_verification;";
        using var conn = _context.CreateConnection();
        var all = await conn.QueryAsync<BusinessVerification>(sql);
        return all.Where(v => v.Level == level).ToList();
    }

    public async Task<List<BusinessVerification>> FindRequiringReverificationAsync()
    {
        const string sql = "SELECT * FROM business_verification WHERE requires_reverification = true;";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessVerification>(sql);
        return results.ToList();
    }
}
