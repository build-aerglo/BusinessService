using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Helpers;

/// <summary>
/// Independent helper class to recalculate verification progress.
/// This can be copied into other services as needed.
/// </summary>
public static class VerificationProgressHelper
{
    /// <summary>
    /// Recalculates and updates the verification progress for a business.
    /// Progress rules:
    /// - idVerified: 30%
    /// - emailVerified: 25%
    /// - phoneVerified: 20%
    /// - addressVerified: 15%
    /// </summary>
    /// <param name="businessId">The business ID to recalculate progress for</param>
    /// <param name="context">The Dapper context for database access</param>
    /// <returns>The calculated verification progress percentage</returns>
    public static async Task<decimal> RecalculateVerificationProgressAsync(Guid businessId, DapperContext context)
    {
        // Fetch the business_verification entry
        const string selectSql = """
            SELECT id_verified, email_verified, phone_verified, address_verified
            FROM business_verification
            WHERE business_id = @BusinessId;
        """;

        using var conn = context.CreateConnection();
        var verification = await conn.QuerySingleOrDefaultAsync<VerificationProgressData>(selectSql, new { BusinessId = businessId });

        if (verification == null)
        {
            return 0;
        }

        // Calculate progress based on rules
        decimal progress = 0;
        if (verification.IdVerified) progress += 30;
        if (verification.EmailVerified) progress += 25;
        if (verification.PhoneVerified) progress += 20;
        if (verification.AddressVerified) progress += 15;

        // Update the verification_progress in the database
        const string updateSql = """
            UPDATE business_verification
            SET verification_progress = @Progress,
                updated_at = @UpdatedAt
            WHERE business_id = @BusinessId;
        """;

        await conn.ExecuteAsync(updateSql, new
        {
            BusinessId = businessId,
            Progress = progress,
            UpdatedAt = DateTime.UtcNow
        });

        return progress;
    }

    private class VerificationProgressData
    {
        public bool IdVerified { get; set; }
        public bool EmailVerified { get; set; }
        public bool PhoneVerified { get; set; }
        public bool AddressVerified { get; set; }
    }
}
