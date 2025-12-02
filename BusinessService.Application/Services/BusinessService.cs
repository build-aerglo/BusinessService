using BusinessService.Application.DTOs;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;

namespace BusinessService.Application.Services;

public class BusinessService : IBusinessService
{
    private readonly IBusinessRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;

    public BusinessService(IBusinessRepository repository, ICategoryRepository categoryRepository, ITagRepository tagRepository)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
    }

    public async Task<BusinessDto> CreateBusinessAsync(CreateBusinessRequest request)
    {
        if (await _repository.ExistsByNameAsync(request.Name))
            throw new BusinessAlreadyExistsException($"Business name '{request.Name}' already exists.");

        var categories = await _categoryRepository.FindAllByIdsAsync(request.CategoryIds);
        if (categories.Count != request.CategoryIds.Count)
            throw new CategoryNotFoundException("One or more categories not found.");

        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Website = request.Website,
            IsBranch = request.ParentBusinessId.HasValue,
            ParentBusinessId = request.ParentBusinessId,
            AvgRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Categories = categories
        };

        await _repository.AddAsync(business);

        return new BusinessDto(
            business.Id,
            business.Name,
            business.Website,
            business.IsBranch,
            business.AvgRating,
            business.ReviewCount,
            business.ParentBusinessId,
            categories.Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.ParentCategoryId)).ToList()
        );
    }
    
    public async Task<BusinessDto> GetBusinessAsync(Guid id)
    {
        var business = await _repository.FindByIdAsync(id)
            ?? throw new BusinessNotFoundException($"Business {id} not found.");

        return new BusinessDto(
            business.Id,
            business.Name,
            business.Website,
            business.IsBranch,
            business.AvgRating,
            business.ReviewCount,
            business.ParentBusinessId,
            business.Categories.Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.ParentCategoryId)).ToList()
        );
    }

    public async Task UpdateRatingsAsync(Guid businessId, decimal newAverage, long newCount)
    {
        var business = await _repository.FindByIdAsync(businessId)
            ?? throw new BusinessNotFoundException($"Business {businessId} not found.");

        business.AvgRating = newAverage;
        business.ReviewCount = newCount;
        business.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(business);

        if (business.ParentBusinessId.HasValue)
            await RecalculateParentRatingAsync(business.ParentBusinessId.Value);
    }

    private async Task RecalculateParentRatingAsync(Guid parentId)
    {
        var branches = await _repository.GetBranchesAsync(parentId);
        if (branches.Count == 0) return;

        var avg = branches.Average(b => b.AvgRating);
        var count = branches.Sum(b => b.ReviewCount);

        var parent = await _repository.FindByIdAsync(parentId);
        if (parent == null) return;

        parent.AvgRating = avg;
        parent.ReviewCount = count;
        parent.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(parent);
    }
    
    public async Task UpdateBusinessAsync(UpdateBusinessRequest request)
    {
        var business = await _repository.FindByIdAsync(request.Id)
                       ?? throw new BusinessNotFoundException($"Business {request.Id} not found.");

        business.Name = request.Name;
        business.Website = request.Website;
        business.Description = request.Description;
        business.UpdatedAt = DateTime.UtcNow;
        
        // Update category
        var categories = await _categoryRepository.FindByIdAsync(request.CategoryId);
        if (categories == null)
            throw new CategoryNotFoundException("Category does not exist.");
        
        var updatedCategory = await _categoryRepository.UpdateBusinessCategoryAsync(request.CategoryId, request.Id); 
        if(!updatedCategory)
            throw new UpdateBusinessFailedException("Failed to update category.");
        
        
        // Update Tags
        // delete existing tags
        var deleteTags = await _tagRepository.DeleteBusinessTagsAsync(request.Id);
        if(!deleteTags)
            throw new UpdateBusinessFailedException("Failed to delete tags.");
        
        // loop through array passed and add tags - hopefully
        foreach (var tagName in request.TagIds)
        {
            // Check if tag exist, break if not
            var tagExists = await _tagRepository.FindByIdAsync(tagName);
            if (tagExists == null)
                continue;

            // add tag
            await _tagRepository.AddBusinessTagAsync(tagName, request.Id);
        }
        
        // Update business
        await _repository.UpdateAsync(business);
    }
}

