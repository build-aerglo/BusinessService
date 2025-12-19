using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;
using Newtonsoft.Json;

namespace BusinessService.Infrastructure.Repositories;

public class BusinessRepository : IBusinessRepository
{
    private readonly DapperContext _context;

    public BusinessRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM business WHERE LOWER(name) = LOWER(@name))";
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { name });
    }

public async Task AddAsync(Business business)
{
    const string sql = """
        INSERT INTO business (
            id,
            name,
            website,
            is_branch,
            avg_rating,
            review_count,
            parent_business_id,
            business_address,
            logo,
            opening_hours,
            business_email,
            business_phone_number,
            cac_number,
            access_username,
            access_number,
            social_media_links,
            business_description,
            media,
            is_verified,
            review_link,
            preferred_contact_method,
            highlights,
            tags,
            average_response_time,
            profile_clicks,
            faqs,
            qr_code_base64,
            created_at,
            updated_at
        )
        VALUES (
            @Id,
            @Name,
            @Website,
            @IsBranch,
            @AvgRating,
            @ReviewCount,
            @ParentBusinessId,
            @BusinessAddress,
            @Logo,
            @OpeningHours,
            @BusinessEmail,
            @BusinessPhoneNumber,
            @CacNumber,
            @AccessUsername,
            @AccessNumber,
            CAST(@SocialMediaLinks AS JSONB),
            @BusinessDescription,
            CAST(@Media AS JSONB),
            @IsVerified,
            @ReviewLink,
            @PreferredContactMethod,
            @Highlights,   -- TEXT[]
            @Tags,         -- TEXT[]
            @AverageResponseTime,
            @ProfileClicks,
            CAST(@Faqs AS JSONB),
            @QrCodeBase64,
            @CreatedAt,
            @UpdatedAt
        );
    """;

    using var conn = _context.CreateConnection();

    await conn.ExecuteAsync(sql, new
    {
        business.Id,
        business.Name,
        business.Website,
        business.IsBranch,
        business.AvgRating,
        business.ReviewCount,
        business.ParentBusinessId,
        business.BusinessAddress,
        business.Logo,

        // JSONB fields
        OpeningHours = business.OpeningHours != null
            ? JsonConvert.SerializeObject(business.OpeningHours)
            : null,

        business.BusinessEmail,
        business.BusinessPhoneNumber,
        business.CacNumber,
        business.AccessUsername,
        business.AccessNumber,

        SocialMediaLinks = business.SocialMediaLinks != null
            ? JsonConvert.SerializeObject(business.SocialMediaLinks)
            : null,

        business.BusinessDescription,

        Media = business.Media != null
            ? JsonConvert.SerializeObject(business.Media)
            : null,

        business.IsVerified,
        business.ReviewLink,
        business.PreferredContactMethod,

        // TEXT[] fields — passed as string[] directly
        Highlights = business.Highlights,
        Tags = business.Tags,

        business.AverageResponseTime,
        business.ProfileClicks,

        Faqs = business.Faqs != null
            ? JsonConvert.SerializeObject(business.Faqs)
            : null,

        business.QrCodeBase64,
        business.CreatedAt,
        business.UpdatedAt
    });

    // Insert category relations
    if (business.Categories.Any())
    {
        const string joinSql = """
            INSERT INTO business_category (business_id, category_id)
            VALUES (@BusinessId, @CategoryId)
            ON CONFLICT DO NOTHING;
        """;

        var joinRows = business.Categories.Select(c => new
        {
            BusinessId = business.Id,
            CategoryId = c.Id
        });

        await conn.ExecuteAsync(joinSql, joinRows);
    }
}



    public async Task<Business?> FindByIdAsync(Guid id)
    {
        const string sql = """
            SELECT * FROM business WHERE id = @id;
        """;

        const string categorySql = """
            SELECT c.* FROM category c
            INNER JOIN business_category bc ON bc.category_id = c.id
            WHERE bc.business_id = @id;
        """;

        using var conn = _context.CreateConnection();
        var business = await conn.QuerySingleOrDefaultAsync<Business>(sql, new { id });
        if (business == null) return null;

        var categories = await conn.QueryAsync<Category>(categorySql, new { id });
        business.Categories = categories.ToList();

        return business;
    }

    public async Task UpdateRatingsAsync(Business business)
    {
        const string sql = """
            UPDATE business
            SET avg_rating = @AvgRating,
                review_count = @ReviewCount,
                updated_at = @UpdatedAt
            WHERE id = @Id;
        """;
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, business);
    }

public async Task UpdateProfileAsync(Business business)
{
    using var conn = _context.CreateConnection();
     conn.Open();
    using var tx = conn.BeginTransaction();

    if (business.Categories != null && business.Categories.Count > 0)
    {
        var categoryIds = business.Categories.Select(c => c.Id).ToArray();

        const string validateSql = """
                                       SELECT id FROM category 
                                       WHERE id = ANY(@Ids);
                                   """;

        var existing = await conn.QueryAsync<Guid>(validateSql, new { Ids = categoryIds }, tx);

        if (existing.Count() != categoryIds.Length)
        {
            var missing = categoryIds.Except(existing).ToList();
            throw new CategoryNotFoundException(
                $"Invalid category IDs: {string.Join(", ", missing)}"
            );
        }
    }
    
    const string sql = """
        UPDATE business
        SET name = @Name,
            website = @Website,
            business_address = @BusinessAddress,
            logo = @Logo,
            opening_hours = CAST(@OpeningHours AS JSONB),
            business_email = @BusinessEmail,
            business_phone_number = @BusinessPhoneNumber,
            cac_number = @CacNumber,
            access_username = @AccessUsername,
            access_number = @AccessNumber,
            social_media_links = CAST(@SocialMediaLinks AS JSONB),
            business_description = @BusinessDescription,
            media = CAST(@Media AS JSONB),
            is_verified = @IsVerified,
            review_link = @ReviewLink,
            preferred_contact_method = @PreferredContactMethod,

            -- FIXED: text[] should not be JSONB
            highlights = @Highlights,
            tags = @Tags,

            average_response_time = @AverageResponseTime,
            profile_clicks = @ProfileClicks,
            faqs = CAST(@Faqs AS JSONB),
            updated_at = @UpdatedAt
        WHERE id = @Id;
    """;

    await conn.ExecuteAsync(sql, new
    {
        business.Id,
        business.Name,
        business.Website,
        business.BusinessAddress,
        business.Logo,

        OpeningHours = business.OpeningHours,
        business.BusinessEmail,
        business.BusinessPhoneNumber,
        business.CacNumber,
        business.AccessUsername,
        business.AccessNumber,

        SocialMediaLinks = business.SocialMediaLinks != null
            ? JsonConvert.SerializeObject(business.SocialMediaLinks)
            : null,

        business.BusinessDescription,

        Media = business.Media != null
            ? JsonConvert.SerializeObject(business.Media)
            : null,

        business.IsVerified,
        business.ReviewLink,
        business.PreferredContactMethod,

        // FIXED — send string[] directly
        Highlights = business.Highlights,
        Tags = business.Tags,

        business.AverageResponseTime,
        business.ProfileClicks,

        Faqs = business.Faqs != null
            ? JsonConvert.SerializeObject(business.Faqs)
            : null,

        business.UpdatedAt
    },tx);
    
    //
    // 3️⃣ UPDATE CATEGORY MAPPINGS
    //
    const string deleteCatSql = """
                                    DELETE FROM business_category WHERE business_id = @BusinessId;
                                """;

    await conn.ExecuteAsync(deleteCatSql, new { BusinessId = business.Id }, tx);

    if (business.Categories.Any())
    {
        const string insertCatSql = """
                                        INSERT INTO business_category (business_id, category_id)
                                        VALUES (@BusinessId, @CategoryId);
                                    """;

        var rows = business.Categories.Select(c => new
        {
            BusinessId = business.Id,
            CategoryId = c.Id
        });

        await conn.ExecuteAsync(insertCatSql, rows, tx);
    }

    tx.Commit();
}


    public async Task<List<Business>> GetBranchesAsync(Guid parentId)
    {
        const string sql = "SELECT * FROM business WHERE parent_business_id = @parentId;";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<Business>(sql, new { parentId });
        return results.ToList();
    }
}
