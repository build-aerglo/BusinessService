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

    public BusinessService(IBusinessRepository repository, ICategoryRepository categoryRepository)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
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
    
    public async Task UpdateBusinessDetailsAsync(Guid id, UpdateBusinessDto dto)
    {
        var business = await _repository.FindByIdAsync(id)
                       ?? throw new BusinessNotFoundException($"Business {id} not found.");

        business.Name = dto.Name;
        business.Website = dto.Website;
        business.UpdatedAt = DateTime.UtcNow;
        // business.Categories = dto.CategoryIds;
        
        // update in memory business
        if (dto.CategoryIds is not null)
        {
            // parse string ids to Guid
            var catGuids = dto.CategoryIds.Select(s => Guid.Parse(s)).ToList();
            var categories = await _categoryRepository.FindAllByIdsAsync(catGuids);
            business.Categories = categories.ToList();
        }

        await _repository.UpdateBusinessDetailsAsync(business, dto.CategoryIds);
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
}

