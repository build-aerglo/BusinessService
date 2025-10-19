using BusinessService.Application.DTOs;

namespace BusinessService.Application.Interfaces;

public interface ICategoryService
{
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request);
    Task<List<CategoryDto>> GetAllTopLevelCategoriesAsync();
    Task<CategoryDto> GetCategoryAsync(Guid id);
    Task<List<CategoryDto>> GetSubCategoriesAsync(Guid parentId);
}