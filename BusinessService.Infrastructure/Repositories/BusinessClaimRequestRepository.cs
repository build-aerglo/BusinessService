using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class BusinessClaimRequestRepository : IBusinessClaimRequestRepository
{
    private readonly DapperContext _context;

    public BusinessClaimRequestRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<BusinessClaimRequest?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM business_claim_request WHERE id = @id;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessClaimRequest>(sql, new { id });
    }

    public async Task<BusinessClaimRequest?> FindPendingByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT * FROM business_claim_request
            WHERE business_id = @businessId
            AND status IN (@pending, @underReview, @moreInfo)
            ORDER BY created_at DESC
            LIMIT 1;
        """;
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BusinessClaimRequest>(sql, new
        {
            businessId,
            pending = (int)ClaimRequestStatus.Pending,
            underReview = (int)ClaimRequestStatus.UnderReview,
            moreInfo = (int)ClaimRequestStatus.MoreInfoRequired
        });
    }

    public async Task<List<BusinessClaimRequest>> FindByBusinessIdAsync(Guid businessId)
    {
        const string sql = "SELECT * FROM business_claim_request WHERE business_id = @businessId ORDER BY created_at DESC;";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessClaimRequest>(sql, new { businessId });
        return results.ToList();
    }

    public async Task<List<BusinessClaimRequest>> FindPendingAsync()
    {
        const string sql = """
            SELECT * FROM business_claim_request
            WHERE status IN (@pending, @underReview, @moreInfo)
            ORDER BY priority DESC, submitted_at;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessClaimRequest>(sql, new
        {
            pending = (int)ClaimRequestStatus.Pending,
            underReview = (int)ClaimRequestStatus.UnderReview,
            moreInfo = (int)ClaimRequestStatus.MoreInfoRequired
        });
        return results.ToList();
    }

    public async Task<List<BusinessClaimRequest>> FindOverdueAsync()
    {
        const string sql = """
            SELECT * FROM business_claim_request
            WHERE status = @pending
            AND expected_review_by < @now
            ORDER BY expected_review_by;
        """;
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessClaimRequest>(sql, new
        {
            pending = (int)ClaimRequestStatus.Pending,
            now = DateTime.UtcNow
        });
        return results.ToList();
    }

    public async Task<List<BusinessClaimRequest>> FindByStatusAsync(ClaimRequestStatus status)
    {
        const string sql = "SELECT * FROM business_claim_request WHERE status = @status ORDER BY created_at DESC;";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessClaimRequest>(sql, new { status = (int)status });
        return results.ToList();
    }

    public async Task AddAsync(BusinessClaimRequest claim)
    {
        const string sql = """
            INSERT INTO business_claim_request (
                id, business_id, claimant_user_id, full_name, email, phone_number, role,
                cac_number, cac_document_url, id_document_url, proof_of_ownership_url, additional_documents_json,
                status, submitted_at, reviewed_at, reviewed_by_user_id, review_notes, rejection_reason,
                cac_verified, id_verified, ownership_verified, contact_verified,
                priority, is_escalated, escalated_at, escalation_reason, expected_review_by,
                created_at, updated_at
            ) VALUES (
                @Id, @BusinessId, @ClaimantUserId, @FullName, @Email, @PhoneNumber, @Role,
                @CacNumber, @CacDocumentUrl, @IdDocumentUrl, @ProofOfOwnershipUrl, @AdditionalDocumentsJson,
                @Status, @SubmittedAt, @ReviewedAt, @ReviewedByUserId, @ReviewNotes, @RejectionReason,
                @CacVerified, @IdVerified, @OwnershipVerified, @ContactVerified,
                @Priority, @IsEscalated, @EscalatedAt, @EscalationReason, @ExpectedReviewBy,
                @CreatedAt, @UpdatedAt
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, claim);
        
        // update business status to in progress
        await UpdateBusinessStatusAsync(claim.Id, "in_progress");
    }

    private async Task UpdateBusinessStatusAsync(Guid id, string status)
    {
        const string sql = """
                               UPDATE business
                               SET business_status = @Status,
                                   updated_at = now()
                               WHERE id = @Id;
                           """;
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new{id, status});
    }
    
    public async Task UpdateAsync(BusinessClaimRequest claim)
    {
        const string sql = """
            UPDATE business_claim_request SET
                claimant_user_id = @ClaimantUserId, full_name = @FullName, email = @Email,
                phone_number = @PhoneNumber, role = @Role, cac_number = @CacNumber,
                cac_document_url = @CacDocumentUrl, id_document_url = @IdDocumentUrl,
                proof_of_ownership_url = @ProofOfOwnershipUrl, additional_documents_json = @AdditionalDocumentsJson,
                status = @Status, reviewed_at = @ReviewedAt, reviewed_by_user_id = @ReviewedByUserId,
                review_notes = @ReviewNotes, rejection_reason = @RejectionReason,
                cac_verified = @CacVerified, id_verified = @IdVerified,
                ownership_verified = @OwnershipVerified, contact_verified = @ContactVerified,
                priority = @Priority, is_escalated = @IsEscalated, escalated_at = @EscalatedAt,
                escalation_reason = @EscalationReason, expected_review_by = @ExpectedReviewBy,
                updated_at = @UpdatedAt
            WHERE id = @Id;
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, claim);
    }

    public async Task<bool> ExistsPendingByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM business_claim_request
                WHERE business_id = @businessId
                AND status IN (@pending, @underReview, @moreInfo)
            );
        """;
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new
        {
            businessId,
            pending = (int)ClaimRequestStatus.Pending,
            underReview = (int)ClaimRequestStatus.UnderReview,
            moreInfo = (int)ClaimRequestStatus.MoreInfoRequired
        });
    }

    public async Task<int> CountPendingAsync()
    {
        const string sql = """
            SELECT COUNT(*) FROM business_claim_request
            WHERE status IN (@pending, @underReview, @moreInfo);
        """;
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            pending = (int)ClaimRequestStatus.Pending,
            underReview = (int)ClaimRequestStatus.UnderReview,
            moreInfo = (int)ClaimRequestStatus.MoreInfoRequired
        });
    }

    public async Task<int> CountOverdueAsync()
    {
        const string sql = """
            SELECT COUNT(*) FROM business_claim_request
            WHERE status = @pending
            AND expected_review_by < @now;
        """;
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            pending = (int)ClaimRequestStatus.Pending,
            now = DateTime.UtcNow
        });
    }
}
