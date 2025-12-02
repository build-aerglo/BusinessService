using BusinessService.Application.DTOs;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;

namespace BusinessService.Application.Services;

public class TagService: ITagService
{
    private readonly ITagRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    
    public async Task<bool> CreateTagAsync(NewTagRequest request)
    {
        // Check if category exists
        var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId);
        if (!categoryExists)
            throw new CategoryNotFoundException("Category does not exist.");

        foreach (var tagName in request.TagNames)
        {
            // Check if tag already exists — skip if true
            var tagExists = await _repository.TagExistAsync(tagName);
            if (tagExists)
                continue;

            // Create new tag
            var tag = new Tags
            {
                Id = Guid.NewGuid(),
                Name = tagName,
                CategoryId = request.CategoryId
            };

            await _repository.AddTagsAsync(tag);
        }

        return true;
    }
    
    public async Task<List<string>> GetCategoryTagsAsync(Guid id)
    {
        var categoryExists = await _categoryRepository.ExistsAsync(id);
        if (!categoryExists)
            throw new CategoryNotFoundException("Category does not exist.");

        return await _repository.GetTagsAsync(id);
    }
}