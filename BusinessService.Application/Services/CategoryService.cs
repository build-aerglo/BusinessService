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

    public async Task<CategoryTagsDto> GetCategoryTagsAsync(Guid categoryId)
    {
        var category = await _repository.FindByIdAsync(categoryId);

        if (category == null)
            throw new CategoryNotFoundException("Category not found.");

        // Fetch domain entities
        var domainTags = await _repository.GetTagsByCategoryIdAsync(categoryId);

        // Map domain → DTO
        var tagDtos = domainTags.Select(t => new TagDto(
            t.Id,
            t.CategoryId,
            t.Name
        )).ToList();

        return new CategoryTagsDto(
            category.Id,
            category.Name,
            tagDtos
        );
    }
    
    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _repository.GetAllCategoriesAsync();

        return categories
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.ParentCategoryId))
            .ToList();
    }

    public async Task<List<CategoryTagsDto>> GetAllCategoriesWithTagsAsync()
    {
        var categories = await _repository.GetAllCategoriesAsync();

        var result = new List<CategoryTagsDto>();

        foreach (var category in categories)
        {
            var domainTags = await _repository.GetTagsByCategoryIdAsync(category.Id);

            var tagDtos = domainTags.Select(t => new TagDto(
                t.Id,
                t.CategoryId,
                t.Name
            )).ToList();

            result.Add(new CategoryTagsDto(
                category.Id,
                category.Name,
                tagDtos
            ));
        }

        return result;
    }

}
