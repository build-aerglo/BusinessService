using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

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

	public async Task UpdateBusinessDetailsAsync(Business business, List<string>? categoryIds)
    {
        const string sql = """
            UPDATE business
            SET name = @Name,
                website = @Website,
                updated_at = @UpdatedAt
            WHERE id = @Id;
        """;
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, business);
        
        if (categoryIds is not null && categoryIds.Any())
        {
            // Clear existing category mappings
            const string deleteSql = """
                                         DELETE FROM business_category
                                         WHERE business_id = @BusinessId;
                                     """;
            await conn.ExecuteAsync(deleteSql, new { BusinessId = business.Id });

            // Reinsert new mappings
            const string insertSql = """
                                         INSERT INTO business_category (business_id, category_id)
                                         VALUES (@BusinessId, @CategoryId)
                                         ON CONFLICT DO NOTHING;
                                     """;

            var joinRows = categoryIds.Select(id => new
            {
                BusinessId = business.Id,
                CategoryId = Guid.Parse(id)
            });

            await conn.ExecuteAsync(insertSql, joinRows);
        }
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

    public async Task<List<Business>> GetBranchesAsync(Guid parentId)
    {
        const string sql = "SELECT * FROM business WHERE parent_business_id = @parentId;";
        using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<Business>(sql, new { parentId });
        return results.ToList();
    }
}
