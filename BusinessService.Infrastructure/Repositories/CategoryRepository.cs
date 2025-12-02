using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly DapperContext _context;

    public CategoryRepository(DapperContext context) => _context = context;
    
    public async Task<Category?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT id, name, description, parent_category_id AS ParentCategoryId FROM category WHERE id = @id;";
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Category>(sql, new { id });
    }

    public async Task<List<Category>> FindAllByIdsAsync(IEnumerable<Guid> ids)
    {
        const string sql = "SELECT id, name, description, parent_category_id AS ParentCategoryId FROM category WHERE id = ANY(@ids);";
        using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<Category>(sql, new { ids = ids.ToArray() });
        return rows.ToList();
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM category WHERE LOWER(name) = LOWER(@name));";
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { name });
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM category WHERE id = @id);";
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { id });
    }

    public async Task AddAsync(Category category)
    {
        const string sql = @"
            INSERT INTO category (id, name, description, parent_category_id, created_at, updated_at)
            VALUES (@Id, @Name, @Description, @ParentCategoryId, now(), now());
        ";
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId
        });
    }

    public async Task<List<Category>> FindTopLevelAsync()
    {
        const string sql = @"
            SELECT id, name, description, parent_category_id AS ParentCategoryId
            FROM category
            WHERE parent_category_id IS NULL
            ORDER BY name;
        ";
        using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<Category>(sql);
        return rows.ToList();
    }

    public async Task<List<Category>> FindSubCategoriesAsync(Guid parentId)
    {
        const string sql = @"
            SELECT id, name, description, parent_category_id AS ParentCategoryId
            FROM category
            WHERE parent_category_id = @parentId
            ORDER BY name;
        ";
        using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<Category>(sql, new { parentId });
        return rows.ToList();
    }
    
    
    public async Task<bool> UpdateBusinessCategoryAsync(Guid id, Guid businessId)
    {
        const string sql = """
                               UPDATE business_category
                               SET category_id = @id
                               WHERE business_id = @businessId;
                           """;
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new {id, businessId});
        
        return true;
    }
}
