using System.Text.Json;
using BusinessService.Application.DTOs;
using BusinessService.Application.DTOs.Subscription;
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
    private readonly ITagRepository _tagRepository;
    private readonly IBusinessVerificationRepository _verificationRepository;
    private readonly ISubscriptionService _subscriptionService;

    public BusinessService(IBusinessRepository repository, ICategoryRepository categoryRepository, IQrCodeService qrCodeService,IBusinessSearchProducer searchProducer,ITagRepository tagRepository, IBusinessVerificationRepository verificationRepository, ISubscriptionService subscriptionService)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _qrCodeService = qrCodeService;
        _searchProducer = searchProducer;
        _tagRepository = tagRepository;
        _verificationRepository = verificationRepository;
        _subscriptionService = subscriptionService;
    }
    
    private async Task<BusinessDto> MapToDto(Business business)
    {
        Dictionary<string, string>? openingHours = null;
        if (!string.IsNullOrWhiteSpace(business.OpeningHours))
        {
            openingHours = JsonSerializer.Deserialize<Dictionary<string, string>>(business.OpeningHours);
        }

        // Convert tag names to TagDto objects
        List<TagDto>? tagDtos = null;
        if (business.Tags != null && business.Tags.Length > 0)
        {
            var tags = await _tagRepository.FindByNamesAsync(business.Tags);
            tagDtos = tags.Select(t => new TagDto(t.Id, t.CategoryId, t.Name)).ToList();
        }

        return new BusinessDto(
            business.Id,
            business.UserId,
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
            tagDtos,
            business.AverageResponseTime,
            business.ProfileClicks,
            business.Faqs?.Select(f => new FaqDto(f.Question, f.Answer)).ToList(),
            business.QrCodeBase64,
            business.BusinessStatus,
            business.BusinessStreet,
            business.BusinessCityTown,
            business.BusinessState,
            business.ReviewSummary,
            business.IdVerified,
            business.IdVerificationType,
            business.BayesianAverage
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
        BusinessEmail = request.Email,
        IsBranch = request.ParentBusinessId.HasValue,
        ParentBusinessId = request.ParentBusinessId,
        AvgRating = 0,
        ReviewCount = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Categories = categories,
        BusinessStatus = request.Status ?? "approved",
        BusinessCityTown = request.BusinessCityTown,
    };

    // *** Generate the QR code content ***
    string qrContent = $"https://clereview.com/biz/{business.Id}";
    business.QrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(qrContent);
    business.ReviewLink = qrContent;

    await _repository.AddAsync(business);

    // Create business_verification entry for the new business
    var verification = new BusinessVerification
    {
        Id = Guid.NewGuid(),
        BusinessId = business.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    await _verificationRepository.AddAsync(verification);

    // Register default subscription for the new business
    try
    {
        await _subscriptionService.CreateSubscriptionAsync(new CreateSubscriptionRequest
        {
            BusinessId = business.Id,
            SubscriptionPlanId = Guid.Parse("cdd7c928-f88f-4a96-b3fc-00dc9b6ecb4f"),
            IsAnnual = false,
            PaymentReference = "initiation"
        });
    }
    catch (Exception)
    {
        // Subscription creation failure should not block business creation
    }

    var dto = await MapToDto(business);
    await _searchProducer.PublishBusinessCreatedAsync(dto);
    return dto;
}


    public async Task<BusinessDto> GetBusinessAsync(Guid id)
    {
        var business = await _repository.FindByIdAsync(id)
            ?? throw new BusinessNotFoundException($"Business {id} not found.");

        return await MapToDto(business);
    }

    public async Task UpdateRatingsAsync(Guid businessId, decimal newAverage, long newCount)
    {
        var business = await _repository.FindByIdAsync(businessId)
                       ?? throw new BusinessNotFoundException($"Business {businessId} not found.");

        business.AvgRating = newAverage;
        business.ReviewCount = newCount;
        business.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateRatingsAsync(business);
        
        var dto = await MapToDto(business);
        await _searchProducer.PublishBusinessUpdatedAsync(dto);

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

        // Track if email or phone changed for verification status update
        // var emailChanged = request.BusinessEmail != null && request.BusinessEmail != business.BusinessEmail;
        var phoneChanged = request.BusinessPhoneNumber != null && request.BusinessPhoneNumber != business.BusinessPhoneNumber;
        var emailChanged = false;
        
        if (request.CategoryIds != null && request.CategoryIds.Count > 0)
        {
            var categories = await _categoryRepository.FindAllByIdsAsync(request.CategoryIds);

            if (categories.Count != request.CategoryIds.Count)
            {
                var invalidIds = request.CategoryIds.Except(categories.Select(c => c.Id));
                throw new CategoryNotFoundException(
                    "Invalid category IDs: " + string.Join(", ", invalidIds)
                );
            }

            business.Categories = categories; // Now contains all valid categories
        }

        business.Name = business.Name;
        business.Website = request.Website ?? business.Website;
        business.BusinessAddress = request.BusinessAddress ?? business.BusinessAddress;
        business.Logo = request.Logo ?? business.Logo;
        if (request.OpeningHours is not null)
        {
            business.OpeningHours = JsonSerializer.Serialize(request.OpeningHours);
        }
        // Note: email update removed - email cannot be updated through profile update
        business.BusinessPhoneNumber = request.BusinessPhoneNumber ?? business.BusinessPhoneNumber;
        business.CacNumber = request.CacNumber ?? business.CacNumber;
        business.AccessUsername = request.AccessUsername ?? business.AccessUsername;
        business.AccessNumber = request.AccessNumber ?? business.AccessNumber;
        business.SocialMediaLinks = request.SocialMediaLinks ?? business.SocialMediaLinks;
        business.BusinessDescription = request.BusinessDescription ?? business.BusinessDescription;
        business.Media = request.Media != null ? new List<string>(request.Media) : business.Media;
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
        business.BusinessStreet = request.BusinessStreet;
        business.BusinessCityTown = request.BusinessCityTown;
        business.BusinessState = request.BusinessState;
        business.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _repository.UpdateProfileAsync(business);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new BusinessConflictException("The provided business email or access username is already in use.");
        }

        // Update business_verification if email or phone changed
        if (emailChanged || phoneChanged)
        {
            var verification = await _verificationRepository.FindByBusinessIdAsync(id);
            if (verification != null)
            {
                await _verificationRepository.UpdatePhoneAndEmailVerificationAsync(
                    id,
                    phoneChanged ? false : verification.PhoneVerified,
                    emailChanged ? false : verification.EmailVerified,
                    phoneChanged ? request.BusinessPhoneNumber : null
                );
            }
        }

        var dto = await MapToDto(business);

        await _searchProducer.PublishBusinessUpdatedAsync(dto);

        return dto;
    }

    public async Task ClaimBusinessAsync(BusinessClaimsDto dto)
    {
        var business = await _repository.FindByIdAsync(dto.Id)
                      ?? throw new BusinessNotFoundException($"Business {dto.Id} not found.");

        switch (business.BusinessStatus)
        {
            case "approved":
                throw new BusinessConflictException("Business already approved.");
            case "in_progress":
                throw new BusinessConflictException("Business approval in progress.");
        }
        
        // add claim
        var claim = new BusinessClaims{Id =dto.Id, Role = dto.Role, Name = dto.Name, Email = dto.Email, Phone = dto.Phone};
        await _repository.ClaimAsync(claim);
    }
    
    public async Task<List<BusinessSummaryResponseDto>> GetBusinessesByCategoryAsync(Guid categoryId)
    {
        // validate category exists
        var category = await _categoryRepository.FindByIdAsync(categoryId);
        if (category == null)
            throw new CategoryNotFoundException($"Category {categoryId} does not exist.");

        var businesses = await _repository.GetBusinessesByCategoryAsync(categoryId);

        var result = new List<BusinessSummaryResponseDto>();
        foreach (var b in businesses)
        {
            // Convert tag names to TagDto objects
            List<TagDto>? tagDtos = null;
            if (b.Tags != null && b.Tags.Length > 0)
            {
                var tags = await _tagRepository.FindByNamesAsync(b.Tags);
                tagDtos = tags.Select(t => new TagDto(t.Id, t.CategoryId, t.Name)).ToList();
            }

            result.Add(new BusinessSummaryResponseDto(
                b.Id,
                b.Name,
                b.AvgRating,
                b.ReviewCount,
                b.IsBranch,
                b.Categories.Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.ParentCategoryId)).ToList(),
                b.BusinessAddress,
                b.Logo,
                b.BusinessPhoneNumber,
                tagDtos,
                b.ReviewSummary,
                b.IsVerified,
                b.BusinessStreet,
                b.BusinessCityTown,
                b.BusinessState,
                b.IdVerified,
                b.IdVerificationType
            ));
        }

        return result;
    }
    public async Task<List<BusinessSummaryResponseDto>> GetBusinessesByTagAsync(Guid tagId)
    {
        // 1. Get the tag → find its category
        var tag = await _tagRepository.FindByIdAsync(tagId);
        if (tag == null)
            throw new TagNotFoundException($"Tag {tagId} not found.");

        var categoryId = tag.CategoryId;

        // 2. Fetch businesses assigned to this category
        var businesses = await _repository.GetBusinessesByCategoryIdAsync(categoryId);
        Console.WriteLine(businesses);

        // 3. Map to DTOs
        var result = new List<BusinessSummaryResponseDto>();
        foreach (var b in businesses)
        {
            // Convert tag names to TagDto objects
            List<TagDto>? tagDtos = null;
            if (b.Tags != null && b.Tags.Length > 0)
            {
                var tags = await _tagRepository.FindByNamesAsync(b.Tags);
                tagDtos = tags.Select(t => new TagDto(t.Id, t.CategoryId, t.Name)).ToList();
            }

            result.Add(new BusinessSummaryResponseDto(
                b.Id,
                b.Name,
                b.AvgRating,
                b.ReviewCount,
                b.IsBranch,
                b.Categories.Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.ParentCategoryId)).ToList(),
                b.BusinessAddress,
                b.Logo,
                b.BusinessPhoneNumber,
                tagDtos,
                b.ReviewSummary,
                b.IsVerified,
                b.BusinessStreet,
                b.BusinessCityTown,
                b.BusinessState,
                b.IdVerified,
                b.IdVerificationType
            ));
        }

        return result;
    }

    private static Dictionary<string, string>? DeserializeOpeningHours(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var result = new Dictionary<string, string>();

        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.String)
            {
                result[prop.Name] = prop.Value.GetString()!;
            }
            else
            {
                // convert objects, arrays, null, numbers, booleans to string
                result[prop.Name] = prop.Value.ToString();
            }
        }

        return result;
    }
    
    // branches
    public async Task<List<BusinessBranches?>> GetBusinessBranchesAsync(Guid businessId)
    {
        _ = await _repository.FindByIdAsync(businessId)
                       ?? throw new BusinessNotFoundException($"Business {businessId} not found.");

        return await _repository.GetBusinessBranchesAsync(businessId);
    }
    
    public async Task<BusinessBranches> AddBranchesAsync(BranchDto dto)
    {
        _ = await _repository.FindByIdAsync(dto.BusinessId)
                       ?? throw new BusinessNotFoundException($"Business {dto.BusinessId} not found.");
        
        var branch = new BusinessBranches{Id = Guid.NewGuid(), BusinessId = dto.BusinessId, BranchName = dto.BranchName, BranchStreet = dto.BranchStreet, BranchCityTown = dto.BranchCityTown, BranchState = dto.BranchState, BranchStatus = "active"};
        await _repository.AddBusinessBranchAsync(branch);
        
        return branch;
    }
    
    public async Task DeleteBranchesAsync(Guid id)
    {
        _ = await _repository.FindBranchByIdAsync(id)
                       ?? throw new BranchNotFoundException("Business branch not found.");

        await _repository.DeleteBusinessBranchAsync(id);
    }

    public async Task<BusinessBranches> UpdateBranchesAsync(BranchUpdateDto dto)
    {
        var branch = await _repository.FindBranchByIdAsync(dto.Id)
                     ?? throw new BranchNotFoundException("Business branch not found.");
        branch.BranchName = dto.BranchName;
        branch.BranchStreet = dto.BranchStreet;
        branch.BranchCityTown = dto.BranchCityTown;
        branch.BranchState = dto.BranchState;

        await _repository.UpdateBusinessBranchAsync(branch);
        return branch;
    }
    
    
    public async Task<BusinessBranches?> GetBranchByIdAsync(Guid branchId)
    {
        var branch = await _repository.FindBranchByIdAsync(branchId);
        if (branch == null)
            throw new BranchNotFoundException($"Branch {branchId} not found.");
    
        return branch;
    }
}

