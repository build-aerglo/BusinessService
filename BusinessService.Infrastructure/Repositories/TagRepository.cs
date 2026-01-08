using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using Dapper;

namespace BusinessService.Infrastructure.Repositories;

public class TagRepository: ITagRepository
{
    
    private readonly DapperContext _context;

    public TagRepository(DapperContext context) => _context = context;
    
    public async Task<Tags?> FindByIdAsync(Guid id)
    {
        const string sql = """
                               SELECT * FROM category_tags WHERE id = @id;
                           """;

        using var conn = _context.CreateConnection();
        var tag = await conn.QuerySingleOrDefaultAsync<Tags>(sql, new { id });
        if (tag == null) return null;

        return tag;
    }
    
    public async Task<bool> DeleteBusinessTagsAsync(Guid id)
    {
        const string sql = @"
            DELETE FROM business_tags WHERE business_id = @id;
        ";
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            id
        });
        return true;
    }
    
    public async Task<bool> TagExistAsync(string name)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM category_tags WHERE LOWER(name) = LOWER(@name));";
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { name });
    }
    
    public async Task<List<string>> GetTagsAsync(Guid id)
    {
        const string sql = "SELECT name FROM category_tags WHERE category_id = @id;";
        using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<string>(sql, new { id });
        return rows.ToList();
    }
    
    public async Task AddTagsAsync(Tags tag)
    {
        const string sql = @"
            INSERT INTO category_tags (id, name, category_id, created_at, updated_at)
            VALUES (@Id, @Name, @CategoryId, now(), now());
        ";
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            tag.Id,
            tag.Name,
            tag.CategoryId
        });
    }
    
    public async Task AddBusinessTagAsync(Guid id, Guid businessId)
    {
        const string sql = @"
            INSERT INTO business_tags (business_id, tag_id, created_at, updated_at)
            VALUES (@businessId, @id, @CategoryId, now(), now());
        ";
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            id,
            businessId
        });
    }

    public async Task<List<Tags>> FindByNamesAsync(string[] names)
    {
        if (names == null || names.Length == 0)
            return new List<Tags>();

        const string sql = "SELECT * FROM category_tags WHERE LOWER(name) = ANY(@names);";
        using var conn = _context.CreateConnection();
        var lowerNames = names.Select(n => n.ToLower()).ToArray();
        var tags = await conn.QueryAsync<Tags>(sql, new { names = lowerNames });
        return tags.ToList();
    }
}