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
            business_status,
            business_street,
            business_citytown,
            business_state,
            id_verified,
            id_verification_url,
            id_verification_type,
            id_verification_number,
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
            @BusinessStatus,
            @BusinessStreet,
            @BusinessCityTown,
            @BusinessState,
            @IdVerified,
            @IdVerificationUrl,
            @IdVerificationType,
            @IdVerificationNumber,
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
        business.BusinessStatus,
        business.BusinessStreet,
        business.BusinessCityTown,
        business.BusinessState,
        business.IdVerified,
        business.IdVerificationUrl,
        business.IdVerificationType,
        business.IdVerificationNumber,
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

    // Insert default auto-response settings
    const string autoResponseSql = """
        INSERT INTO business_auto_response (business_id, positive_response, negative_response, neutral_response, allow_auto_response)
        VALUES (@BusinessId, NULL, NULL, NULL, false)
        ON CONFLICT DO NOTHING;
    """;
    await conn.ExecuteAsync(autoResponseSql, new { BusinessId = business.Id });
}


public async Task UpdateBusinessStatusAsync(Guid id, string status)
{
    const string sql = """
                           UPDATE business
                           SET business_status = @Status,
                               updated_at = now()
                           WHERE id = @Id;
                       """;
    using var conn = _context.CreateConnection();
    await conn.ExecuteAsync(sql, new { Id = id, Status = status });
}
    
public async Task ClaimAsync(BusinessClaims claim)
{
    const string sql = @"
            INSERT INTO business_claims (business_id, name, role, email, phone, verified, created_at, updated_at)
            VALUES (@Id, @Name, @Role, @Email, @Phone, false, now(), now());
        ";
    using var conn = _context.CreateConnection();
    await conn.ExecuteAsync(sql, new
    {
        claim.Id,
        claim.Name,
        claim.Role,
        claim.Email,
        claim.Phone,
    });
        
    // update business status to in progress
    await UpdateBusinessStatusAsync(claim.Id, "in_progress");
}

    public async Task<Business?> FindByIdAsync(Guid id)
    {
        const string sql = """
                               SELECT 
                                   b.*,
                                   br.bayesian_average as BayesianAverage  
                               FROM business b
                               LEFT JOIN business_rating br ON b.id = br.business_id  
                               WHERE b.id = @id;
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
            business_street = @BusinessStreet,
            business_citytown = @BusinessCityTown,
            business_state = @BusinessState,
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
        business.BusinessStreet,
        business.BusinessCityTown,
        business.BusinessState,

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
    
    public async Task<List<Business>> GetBusinessesByCategoryAsync(Guid categoryId)
    {
        const string businessSql = """
                                       SELECT DISTINCT 
                                           b.*,
                                           br.bayesian_average as BayesianAverage  
                                       FROM business b
                                       INNER JOIN business_category bc ON bc.business_id = b.id
                                       LEFT JOIN business_rating br ON b.id = br.business_id  
                                       WHERE bc.category_id = @categoryId;
                                   """;

        const string categorySql = """
                                       SELECT bc.business_id, c.*
                                       FROM category c
                                       INNER JOIN business_category bc ON bc.category_id = c.id
                                       WHERE bc.business_id = ANY(@businessIds);
                                   """;

        using var conn = _context.CreateConnection();

        var businesses = (await conn.QueryAsync<Business>(
            businessSql,
            new { categoryId }
        )).ToList();

        if (!businesses.Any())
            return businesses;

        var businessIds = businesses.Select(b => b.Id).ToArray();

        var categoryLookup = new Dictionary<Guid, List<Category>>();

        var rows = await conn.QueryAsync<Guid, Category, (Guid, Category)>(
            categorySql,
            (businessId, category) => (businessId, category),
            new { businessIds },
            splitOn: "id"
        );

        foreach (var (businessId, category) in rows)
        {
            if (!categoryLookup.TryGetValue(businessId, out var list))
            {
                list = new List<Category>();
                categoryLookup[businessId] = list;
            }
            list.Add(category);
        }

        foreach (var business in businesses)
        {
            business.Categories = categoryLookup.TryGetValue(business.Id, out var cats)
                ? cats
                : new List<Category>();
        }

        return businesses;
    }

    
    public async Task<List<Business>> GetBusinessesByCategoryIdAsync(Guid categoryId)
    {
        const string sql = """
                               SELECT
                                   b.*,
                                   br.bayesian_average as BayesianAverage, 
                                   c.id,
                                   c.name
                               FROM business b
                               JOIN business_category bc ON bc.business_id = b.id
                               JOIN category c ON c.id = bc.category_id
                               LEFT JOIN business_rating br ON b.id = br.business_id  
                               WHERE c.id = @categoryId;
                           """;

        using var conn = _context.CreateConnection();

        var lookup = new Dictionary<Guid, Business>();

        var result = await conn.QueryAsync<Business, Category, Business>(
            sql,
            (business, category) =>
            {
                if (!lookup.TryGetValue(business.Id, out var b))
                {
                    b = business;
                    b.Categories = new List<Category>();
                    lookup[b.Id] = b;
                }

                b.Categories.Add(category);
                return b;
            },
            new { categoryId },
            splitOn: "id"
        );

        return lookup.Values.ToList();
    }

    // branch repos
    public async Task<List<BusinessBranches?>> GetBusinessBranchesAsync(Guid parentId)
    {
        const string sql = "SELECT * FROM business_branches WHERE business_id = @parentId AND branch_status = 'active';";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<BusinessBranches>(sql, new { parentId });
        return results.ToList();
    }

    public async Task AddBusinessBranchAsync(BusinessBranches branch)
    {
        const string sql = @"
            INSERT INTO business_branches (business_id, id, branch_name, branch_street, branch_citytown, branch_state, created_at, updated_at)
            VALUES (@BusinessId, @Id, @BranchName, @BranchStreet, @BranchCityTown, @BranchState, now(), now());
        ";
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            branch.BusinessId,
            branch.Id,
            branch.BranchName,
            branch.BranchStreet,
            branch.BranchCityTown,
            branch.BranchState
        });
    }
    
    public async Task DeleteBusinessBranchAsync(Guid id)
    {
        const string sql = """
                               UPDATE business_branches
                               SET branch_status = 'inactive',
                                   updated_at = now()
                               WHERE id = @Id;
                           """;
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new{id});
    }
    
    public async Task UpdateBusinessBranchAsync(BusinessBranches branch)
    {
        const string sql = """
                               UPDATE business_branches
                               SET branch_name = @BranchName,
                                   branch_street = @BranchStreet,
                                   branch_citytown = @BranchCityTown,
                                   branch_state = @BranchState,
                                   updated_at = now()
                               WHERE id = @Id;
                           """;
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            branch.Id,
            branch.BranchName,
            branch.BranchStreet,
            branch.BranchCityTown,
            branch.BranchState
        });
    }
    
    public async Task<BusinessBranches?> FindBranchByIdAsync(Guid id)
    {
        const string sql = """
                               SELECT * FROM business_branches WHERE id = @id;
                           """;

        using var conn = _context.CreateConnection();
        var branch = await conn.QuerySingleOrDefaultAsync<BusinessBranches>(sql, new { id });
        if (branch == null) return null;

        return branch;
    }

    public async Task UpdateIdVerificationAsync(Guid businessId, string idVerificationUrl, string idVerificationType, string idVerificationNumber)
    {
        const string sql = """
                               UPDATE business
                               SET id_verified = false,
                                   id_verification_url = @IdVerificationUrl,
                                   id_verification_type = @IdVerificationType,
                                   id_verification_number = @IdVerificationNumber,
                                   updated_at = @UpdatedAt
                               WHERE id = @BusinessId;
                           """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            BusinessId = businessId,
            IdVerificationUrl = idVerificationUrl,
            IdVerificationType = idVerificationType,
            IdVerificationNumber = idVerificationNumber,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task UpdatePreferredContactMethodAsync(Guid businessId, string preferredContactMethod)
    {
        const string sql = """
                               UPDATE business
                               SET preferred_contact_method = @PreferredContactMethod,
                                   updated_at = @UpdatedAt
                               WHERE id = @BusinessId;
                           """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            BusinessId = businessId,
            PreferredContactMethod = preferredContactMethod,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<Guid?> GetBusinessUserIdByBusinessIdAsync(Guid businessId)
    {
        const string sql = """
                               SELECT u.id
                               FROM users u
                               WHERE u.email = (
                                   SELECT b.business_email FROM business b WHERE b.id = @BusinessId
                               )
                               AND u.user_type = 'business_user'
                               LIMIT 1;
                           """;

        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Guid?>(sql, new { BusinessId = businessId });
    }
}
