using System.Text.Json;
using BusinessService.Application.DTOs;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using Npgsql;

namespace BusinessService.Application.Services;

public class BusinessService : IBusinessService
{
    private readonly IBusinessRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IQrCodeService _qrCodeService;
    private readonly IBusinessSearchProducer _searchProducer;

    public BusinessService(IBusinessRepository repository, ICategoryRepository categoryRepository, IQrCodeService qrCodeService,IBusinessSearchProducer searchProducer)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _qrCodeService = qrCodeService;
        _searchProducer = searchProducer;
    }
    
    private static BusinessDto MapToDto(Business business)
    {
        Dictionary<string, string>? openingHours = null;
        if (!string.IsNullOrWhiteSpace(business.OpeningHours))
        {
            openingHours = JsonSerializer.Deserialize<Dictionary<string, string>>(business.OpeningHours);
        }
        return new BusinessDto(
            business.Id,
            business.Name,
            business.Website,
            business.IsBranch,
            business.AvgRating,
            business.ReviewCount,
            business.ParentBusinessId,
            business.Categories.Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.ParentCategoryId
            )).ToList(),
            business.BusinessAddress,
            business.Logo,
            openingHours,
            business.BusinessEmail,
            business.BusinessPhoneNumber,
            business.CacNumber,
            business.AccessUsername,
            business.AccessNumber,
            business.SocialMediaLinks,
            business.BusinessDescription,
            business.Media,
            business.IsVerified,
            business.ReviewLink,
            business.PreferredContactMethod,
            business.Highlights,
            business.Tags,
            business.AverageResponseTime,
            business.ProfileClicks,
            business.Faqs?.Select(f => new FaqDto(f.Question, f.Answer)).ToList(),
            business.QrCodeBase64
        );
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

    // *** Generate the QR code content ***
    string qrContent = $"https://clereview.com/business/{business.Id}";
    business.QrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(qrContent);
    business.ReviewLink = qrContent;

    await _repository.AddAsync(business);

    var dto = MapToDto(business);
    await _searchProducer.PublishBusinessCreatedAsync(dto);
    return dto;
}


    public async Task<BusinessDto> GetBusinessAsync(Guid id)
    {
        var business = await _repository.FindByIdAsync(id)
            ?? throw new BusinessNotFoundException($"Business {id} not found.");
        
        // Deserialize opening hours ONLY for API output
        Dictionary<string, string>? openingHours = null;
        if (!string.IsNullOrWhiteSpace(business.OpeningHours))
        {
            openingHours = JsonSerializer.Deserialize<Dictionary<string, string>>(business.OpeningHours);
        }


        return new BusinessDto(
            business.Id,
            business.Name,
            business.Website,
            business.IsBranch,
            business.AvgRating,
            business.ReviewCount,
            business.ParentBusinessId,
            business.Categories.Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.ParentCategoryId)).ToList(),
            business.BusinessAddress,
            business.Logo,
            openingHours,
            business.BusinessEmail,
            business.BusinessPhoneNumber,
            business.CacNumber,
            business.AccessUsername,
            business.AccessNumber,
            business.SocialMediaLinks,
            business.BusinessDescription,
            business.Media,
            business.IsVerified,
            business.ReviewLink,
            business.PreferredContactMethod,
            business.Highlights,
            business.Tags,
            business.AverageResponseTime,
            business.ProfileClicks,
            business.Faqs?.Select(f => new FaqDto(f.Question, f.Answer)).ToList(),
            business.QrCodeBase64
        );
    }

    public async Task UpdateRatingsAsync(Guid businessId, decimal newAverage, long newCount)
    {
        var business = await _repository.FindByIdAsync(businessId)
            ?? throw new BusinessNotFoundException($"Business {businessId} not found.");

        business.AvgRating = newAverage;
        business.ReviewCount = newCount;
        business.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateRatingsAsync(business);

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

        await _repository.UpdateRatingsAsync(parent);
    }

    public async Task<BusinessDto> UpdateBusinessAsync(Guid id, UpdateBusinessRequest request)
    {
        var business = await _repository.FindByIdAsync(id)
            ?? throw new BusinessNotFoundException($"Business {id} not found.");

        business.Name = request.Name ?? business.Name;
        business.Website = request.Website ?? business.Website;
        business.BusinessAddress = request.BusinessAddress ?? business.BusinessAddress;
        business.Logo = request.Logo ?? business.Logo;
        if (request.OpeningHours is not null)
        {
            business.OpeningHours = JsonSerializer.Serialize(request.OpeningHours);
        }
        business.BusinessEmail = request.BusinessEmail ?? business.BusinessEmail;
        business.BusinessPhoneNumber = request.BusinessPhoneNumber ?? business.BusinessPhoneNumber;
        business.CacNumber = request.CacNumber ?? business.CacNumber;
        business.AccessUsername = request.AccessUsername ?? business.AccessUsername;
        business.AccessNumber = request.AccessNumber ?? business.AccessNumber;
        business.SocialMediaLinks = request.SocialMediaLinks ?? business.SocialMediaLinks;
        business.BusinessDescription = request.BusinessDescription ?? business.BusinessDescription;
        business.Media = request.Media ?? business.Media;
        if (request.IsVerified.HasValue)
        {
            business.IsVerified = request.IsVerified.Value;
        }
        business.PreferredContactMethod = request.PreferredContactMethod ?? business.PreferredContactMethod;
        business.Highlights = request.Highlights ?? business.Highlights;
        business.Tags = request.Tags ?? business.Tags;
        business.AverageResponseTime = request.AverageResponseTime ?? business.AverageResponseTime;
        if (request.ProfileClicks.HasValue)
        {
            business.ProfileClicks = request.ProfileClicks.Value;
        }
        business.Faqs = request.Faqs?
                            .Select(f => new Faq(f.Question, f.Answer))
                            .ToList()
                        ?? business.Faqs;
        business.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _repository.UpdateProfileAsync(business);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new BusinessConflictException("The provided business email or access username is already in use.");
        }
        
        var dto = MapToDto(business);
        
        await _searchProducer.PublishBusinessUpdatedAsync(dto);

        return dto;
    }
}

