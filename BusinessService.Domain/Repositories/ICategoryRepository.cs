using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface ICategoryRepository
{
    // existing
    Task<Category?> FindByIdAsync(Guid id);
    Task<List<Category>> FindAllByIdsAsync(IEnumerable<Guid> ids);

    // needed by services
    Task<bool> ExistsByNameAsync(string name);
    Task<bool> ExistsAsync(Guid id);
    Task AddAsync(Category category);
    Task<List<Category>> FindTopLevelAsync();
    Task<List<Category>> FindSubCategoriesAsync(Guid parentId);
    Task<List<Tags>> GetTagsByCategoryIdAsync(Guid categoryId);
    Task<List<Category>> GetAllCategoriesAsync();
    
}