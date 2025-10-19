using BusinessService.Application.DTOs;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;

namespace BusinessService.Application.Services;


public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request)
    {
        // Check if name already exists (case-insensitive)
        var exists = await _repository.ExistsByNameAsync(request.Name);
        if (exists)
            throw new CategoryAlreadyExistsException($"Category name '{request.Name}' already exists.");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId
        };

        if (request.ParentCategoryId.HasValue)
        {
            var parent = await _repository.FindByIdAsync(request.ParentCategoryId.Value);
            if (parent == null)
                throw new CategoryNotFoundException($"Parent category with ID {request.ParentCategoryId} not found.");
        }

        await _repository.AddAsync(category);

        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId
        );
    }

    public async Task<List<CategoryDto>> GetAllTopLevelCategoriesAsync()
    {
        var topCategories = await _repository.FindTopLevelAsync();

        return topCategories
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.ParentCategoryId))
            .ToList();
    }

    public async Task<CategoryDto> GetCategoryAsync(Guid id)
    {
        var category = await _repository.FindByIdAsync(id)
            ?? throw new CategoryNotFoundException($"Category {id} not found.");

        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId
        );
    }

    public async Task<List<CategoryDto>> GetSubCategoriesAsync(Guid parentId)
    {
        var parentExists = await _repository.ExistsAsync(parentId);
        if (!parentExists)
            throw new CategoryNotFoundException($"Parent category with ID {parentId} not found.");

        var subCategories = await _repository.FindSubCategoriesAsync(parentId);

        return subCategories
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.ParentCategoryId))
            .ToList();
    }
}
