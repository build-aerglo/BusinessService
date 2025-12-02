using BusinessService.Domain.Entities;
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
                id, name, website, is_branch, avg_rating, review_count,
                parent_business_id, created_at, updated_at
            ) VALUES (
                @Id, @Name, @Website, @IsBranch, @AvgRating, @ReviewCount,
                @ParentBusinessId, @CreatedAt, @UpdatedAt
            );
        """;

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, business);

        if (business.Categories.Any())
        {
            const string joinSql = """
                INSERT INTO business_category (business_id, category_id)
                VALUES (@BusinessId, @CategoryId)
                ON CONFLICT DO NOTHING;
            """;

            var joinRows = business.Categories.Select(c => new { BusinessId = business.Id, CategoryId = c.Id });
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

    public async Task UpdateAsync(Business business)
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
                media = @Media,
                is_verified = @IsVerified,
                review_link = @ReviewLink,
                preferred_contact_method = @PreferredContactMethod,
                updated_at = @UpdatedAt
            WHERE id = @Id;
        """;
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            business.Id,
            business.Name,
            business.Website,
            business.BusinessAddress,
            business.Logo,
            OpeningHours = JsonConvert.SerializeObject(business.OpeningHours),
            business.BusinessEmail,
            business.BusinessPhoneNumber,
            business.CacNumber,
            business.AccessUsername,
            business.AccessNumber,
            SocialMediaLinks = JsonConvert.SerializeObject(business.SocialMediaLinks),
            business.BusinessDescription,
            business.Media,
            business.IsVerified,
            business.ReviewLink,
            business.PreferredContactMethod,
            business.UpdatedAt
        });
    }

    public async Task<List<Business>> GetBranchesAsync(Guid parentId)
    {
        const string sql = "SELECT * FROM business WHERE parent_business_id = @parentId;";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<Business>(sql, new { parentId });
        return results.ToList();
    }
}
