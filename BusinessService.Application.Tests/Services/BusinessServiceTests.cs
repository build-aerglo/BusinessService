// BusinessService.Application.Tests/Services/BusinessServiceTests.cs
// ✅ FIXED: Nullable decimal issues

using BusinessService.Application.DTOs;
using BusinessService.Application.Interfaces;
using BusinessService.Application.Services;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using FluentAssertions;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace BusinessService.Application.Tests.Services;

[TestFixture]
public class BusinessServiceTests
{
    private Mock<IBusinessRepository> _businessRepoMock = null!;
    private Mock<ICategoryRepository> _categoryRepoMock = null!;
    private Mock<IQrCodeService> _qrCodeServiceMock = null!;
    private Mock<IBusinessSearchProducer> _searchProducerMock = null!;
    private Application.Services.BusinessService _service = null!;
    private Mock<ITagRepository> _tagRepoMock = null!;
    
    [SetUp]
    public void Setup()
    {
        _businessRepoMock = new Mock<IBusinessRepository>();
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _qrCodeServiceMock = new Mock<IQrCodeService>();
        _searchProducerMock = new Mock<IBusinessSearchProducer>();
        _tagRepoMock = new Mock<ITagRepository>();

        _qrCodeServiceMock.Setup(q => q.GenerateQrCodeBase64(It.IsAny<string>()))
            .Returns("BASE64_QR_CODE_TEST_VALUE");

        _service = new Application.Services.BusinessService(
            _businessRepoMock.Object,
            _categoryRepoMock.Object,
            _qrCodeServiceMock.Object,
            _searchProducerMock.Object,
            _tagRepoMock.Object
        );
    }

    [Test]
    public async Task CreateBusinessAsync_ShouldCreateBusiness_WhenValid()
    {
        var request = new CreateBusinessRequest
        {
            Name = "Alpha Coffee",
            Website = "https://alphacoffee.com",
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        };

        var category = new Category { Id = request.CategoryIds[0], Name = "Coffee" };

        _businessRepoMock.Setup(r => r.ExistsByNameAsync(request.Name)).ReturnsAsync(false);
        _categoryRepoMock.Setup(r => r.FindAllByIdsAsync(request.CategoryIds))
            .ReturnsAsync(new List<Category> { category });
        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        var result = await _service.CreateBusinessAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Alpha Coffee");
        result.Categories.Should().ContainSingle(c => c.Name == "Coffee");
        result.QrCodeBase64.Should().Be("BASE64_QR_CODE_TEST_VALUE");
        
        // ✅ FIXED: BayesianAverage should be null for new business (no reviews yet)
        result.BayesianAverage.Should().BeNull();

        _businessRepoMock.Verify(r => r.AddAsync(It.IsAny<Business>()), Times.Once);
        _qrCodeServiceMock.Verify(q => q.GenerateQrCodeBase64(It.IsAny<string>()), Times.Once);
        _searchProducerMock.Verify(s => s.PublishBusinessCreatedAsync(It.IsAny<BusinessDto>()), Times.Once);
    }

    [Test]
    public void CreateBusinessAsync_ShouldThrow_WhenNameAlreadyExists()
    {
        var request = new CreateBusinessRequest
        {
            Name = "Duplicate Shop",
            Website = "https://dup.com",
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        };

        _businessRepoMock.Setup(r => r.ExistsByNameAsync(request.Name)).ReturnsAsync(true);

        Func<Task> act = async () => await _service.CreateBusinessAsync(request);

        act.Should().ThrowAsync<BusinessAlreadyExistsException>()
            .WithMessage("Business name 'Duplicate Shop' already exists.");
    }

    [Test]
    public void CreateBusinessAsync_ShouldThrow_WhenCategoryNotFound()
    {
        var request = new CreateBusinessRequest
        {
            Name = "Shop X",
            Website = "https://shopx.com",
            CategoryIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        _businessRepoMock.Setup(r => r.ExistsByNameAsync(request.Name)).ReturnsAsync(false);
        _categoryRepoMock.Setup(r => r.FindAllByIdsAsync(request.CategoryIds))
            .ReturnsAsync(new List<Category> { new Category { Id = request.CategoryIds[0] } });

        Func<Task> act = async () => await _service.CreateBusinessAsync(request);

        act.Should().ThrowAsync<CategoryNotFoundException>()
            .WithMessage("One or more categories not found.");
    }

    [Test]
    public async Task GetBusinessAsync_ShouldReturnBusinessWithBayesianAverage_WhenBusinessHasReviews()
    {
        var businessId = Guid.NewGuid();
        var business = new Business
        {
            Id = businessId,
            Name = "Test Business",
            AvgRating = 4.20m,
            ReviewCount = 15,
            BayesianAverage = 4.12m, // ✅ Populated from LEFT JOIN
            Categories = new List<Category>()
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(business);
        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        var result = await _service.GetBusinessAsync(businessId);

        result.Should().NotBeNull();
        result.AvgRating.Should().Be(4.20m);
        result.ReviewCount.Should().Be(15);
        result.BayesianAverage.Should().Be(4.12m); // ✅ Verify Bayesian is returned
    }

    [Test]
    public async Task GetBusinessAsync_ShouldReturnBusinessWithNullBayesianAverage_WhenBusinessHasNoReviews()
    {
        var businessId = Guid.NewGuid();
        var business = new Business
        {
            Id = businessId,
            Name = "New Business",
            AvgRating = 0,
            ReviewCount = 0,
            BayesianAverage = null, // ✅ NULL when no reviews (LEFT JOIN returns null)
            Categories = new List<Category>()
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(business);
        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        var result = await _service.GetBusinessAsync(businessId);

        result.Should().NotBeNull();
        result.AvgRating.Should().Be(0);
        result.ReviewCount.Should().Be(0);
        result.BayesianAverage.Should().BeNull(); // ✅ NULL is valid
    }

    [Test]
    public async Task UpdateBusinessAsync_ShouldUpdateBusiness_WhenValid()
    {
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };
        var business = new Business 
        { 
            Id = id, 
            Name = "Old Name",
            BayesianAverage = 3.85m // ✅ Business may have existing rating
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(business);
        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        var result = await _service.UpdateBusinessAsync(id, request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.BayesianAverage.Should().Be(3.85m); // ✅ Rating preserved

        _businessRepoMock.Verify(r => r.UpdateProfileAsync(It.Is<Business>(b => b.Name == "Updated Name")), Times.Once);
        _qrCodeServiceMock.Verify(q => q.GenerateQrCodeBase64(It.IsAny<string>()), Times.Never);
        _searchProducerMock.Verify(s => s.PublishBusinessUpdatedAsync(It.IsAny<BusinessDto>()), Times.Once);
    }

    [Test]
    public void UpdateBusinessAsync_ShouldThrow_WhenBusinessNotFound()
    {
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((Business?)null);

        Func<Task> act = async () => await _service.UpdateBusinessAsync(id, request);

        act.Should().ThrowAsync<BusinessNotFoundException>()
            .WithMessage($"Business {id} not found.");
    }

    [Test]
    public void UpdateBusinessAsync_ShouldThrow_WhenUniqueConstraintViolated()
    {
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };
        var business = new Business { Id = id, Name = "Old Name" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(business);
        _businessRepoMock.Setup(r => r.UpdateProfileAsync(It.IsAny<Business>()))
            .ThrowsAsync(new PostgresException("msg", "severity", "23505", "detail"));

        Func<Task> act = async () => await _service.UpdateBusinessAsync(id, request);

        act.Should().ThrowAsync<BusinessConflictException>()
            .WithMessage("The provided business email or access username is already in use.");
    }

    [Test]
    public async Task UpdateRatingsAsync_ShouldUpdateBusinessRatings_AndPublishEvent()
    {
        var businessId = Guid.NewGuid();
        var business = new Business
        {
            Id = businessId,
            Name = "Test Business",
            AvgRating = 3.5m,
            ReviewCount = 10,
            BayesianAverage = 3.44m,
            Categories = new List<Category>()
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(business);
        _businessRepoMock.Setup(r => r.UpdateRatingsAsync(It.IsAny<Business>()))
            .Returns(Task.CompletedTask);
        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        await _service.UpdateRatingsAsync(businessId, 4.2m, 15);

        _businessRepoMock.Verify(r => r.UpdateRatingsAsync(
            It.Is<Business>(b => 
                b.Id == businessId && 
                b.AvgRating == 4.2m && 
                b.ReviewCount == 15)
        ), Times.Once);

        _searchProducerMock.Verify(s => s.PublishBusinessUpdatedAsync(
            It.Is<BusinessDto>(dto => 
                dto.Id == businessId &&
                dto.AvgRating == 4.2m &&
                dto.ReviewCount == 15)
        ), Times.Once);
    }
    
    [Test]
    public void GetBusinessesByCategory_ShouldThrow_WhenCategoryDoesNotExist()
    {
        var categoryId = Guid.NewGuid();

        _categoryRepoMock
            .Setup(r => r.FindByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        Assert.ThrowsAsync<CategoryNotFoundException>(() =>
            _service.GetBusinessesByCategoryAsync(categoryId));
    }

    [Test]
    public async Task GetBusinessesByCategory_ShouldReturnBusinesses_WhenCategoryExists()
    {
        var categoryId = Guid.NewGuid();

        _categoryRepoMock
            .Setup(r => r.FindByIdAsync(categoryId))
            .ReturnsAsync(new Category
            {
                Id = categoryId,
                Name = "Repairs"
            });

        _businessRepoMock
            .Setup(r => r.GetBusinessesByCategoryAsync(categoryId))
            .ReturnsAsync(new List<Business>
            {
                new Business
                {
                    Id = Guid.NewGuid(),
                    Name = "FixIt Hub",
                    AvgRating = 4.2m,
                    ReviewCount = 120,
                    BayesianAverage = 4.15m,
                    Categories = new List<Category>()
                },
                new Business
                {
                    Id = Guid.NewGuid(),
                    Name = "Acme Repair Co",
                    AvgRating = 4.7m,
                    ReviewCount = 300,
                    BayesianAverage = 4.68m,
                    Categories = new List<Category>()
                }
            });

        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        var result = await _service.GetBusinessesByCategoryAsync(categoryId);

        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result[0].Name.Should().Be("FixIt Hub");
        result[1].AvgRating.Should().Be(4.7m);
    }

    [Test]
    public async Task GetBusinessesByCategory_ShouldReturnEmptyList_WhenNoBusinessesFound()
    {
        var categoryId = Guid.NewGuid();

        _categoryRepoMock
            .Setup(r => r.FindByIdAsync(categoryId))
            .ReturnsAsync(new Category
            {
                Id = categoryId,
                Name = "Repairs"
            });

        _businessRepoMock
            .Setup(r => r.GetBusinessesByCategoryAsync(categoryId))
            .ReturnsAsync(new List<Business>());

        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        var result = await _service.GetBusinessesByCategoryAsync(categoryId);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
    
    [Test]
    public async Task ClaimBusinessAsync_ShouldClaimBusiness_WhenBusinessExistsAndNotClaimed()
    {
        var businessId = Guid.NewGuid();
        var dto = new BusinessClaimsDto
        (
            businessId,
            "John Doe",
            "Owner",
            "john@example.com",
            "+1234567890"
        );

        var business = new Business
        {
            Id = businessId,
            Name = "Test Business",
            BusinessStatus = "pending"
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(business);
        _businessRepoMock.Setup(r => r.ClaimAsync(It.IsAny<BusinessClaims>()))
            .Returns(Task.CompletedTask);

        await _service.ClaimBusinessAsync(dto);

        _businessRepoMock.Verify(r => r.FindByIdAsync(businessId), Times.Once);
        _businessRepoMock.Verify(r => r.ClaimAsync(It.Is<BusinessClaims>(c =>
            c.Id == businessId &&
            c.Role == "Owner" &&
            c.Name == "John Doe" &&
            c.Email == "john@example.com" &&
            c.Phone == "+1234567890"
        )), Times.Once);
    }

    [Test]
    public void ClaimBusinessAsync_ShouldThrowBusinessNotFoundException_WhenBusinessDoesNotExist()
    {
        var businessId = Guid.NewGuid();
        var dto = new BusinessClaimsDto
        (
            businessId,
            "John Doe",
            "Owner",
            "john@example.com",
            "+1234567890"
        );

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync((Business?)null);

        Func<Task> act = async () => await _service.ClaimBusinessAsync(dto);

        act.Should().ThrowAsync<BusinessNotFoundException>()
            .WithMessage($"Business {businessId} not found.");
        _businessRepoMock.Verify(r => r.ClaimAsync(It.IsAny<BusinessClaims>()), Times.Never);
    }

    [Test]
    public void ClaimBusinessAsync_ShouldThrowBusinessConflictException_WhenBusinessIsApproved()
    {
        var businessId = Guid.NewGuid();
        var dto = new BusinessClaimsDto
        (
            businessId,
            "John Doe",
            "Owner",
            "john@example.com",
            "+1234567890"
        );

        var business = new Business
        {
            Id = businessId,
            Name = "Test Business",
            BusinessStatus = "approved"
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(business);

        Func<Task> act = async () => await _service.ClaimBusinessAsync(dto);

        act.Should().ThrowAsync<BusinessConflictException>()
            .WithMessage("Business already approved.");
        _businessRepoMock.Verify(r => r.ClaimAsync(It.IsAny<BusinessClaims>()), Times.Never);
    }

    [Test]
    public void ClaimBusinessAsync_ShouldThrowBusinessConflictException_WhenBusinessIsInProgress()
    {
        var businessId = Guid.NewGuid();
        var dto = new BusinessClaimsDto
        (
            businessId,
            "John Doe",
            "Owner",
            "john@example.com",
            "+1234567890"
        );

        var business = new Business
        {
            Id = businessId,
            Name = "Test Business",
            BusinessStatus = "in_progress"
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(business);

        Func<Task> act = async () => await _service.ClaimBusinessAsync(dto);

        act.Should().ThrowAsync<BusinessConflictException>()
            .WithMessage("Business approval in progress.");
        _businessRepoMock.Verify(r => r.ClaimAsync(It.IsAny<BusinessClaims>()), Times.Never);
    }
    
    [Test]
    public async Task GetBusinessBranchesAsync_ReturnsBranches_WhenBusinessExists()
    {
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId };

        var branches = new List<BusinessBranches?>
        {
            new BusinessBranches { Id = Guid.NewGuid(), BusinessId = businessId }
        };

        _businessRepoMock
            .Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(business);
        _businessRepoMock
            .Setup(r => r.GetBusinessBranchesAsync(businessId))
            .ReturnsAsync(branches);

        var result = await _service.GetBusinessBranchesAsync(businessId);

        result.Should().BeEquivalentTo(branches);
    }

    [Test]
    public async Task GetBusinessBranchesAsync_Throws_WhenBusinessNotFound()
    {
        var businessId = Guid.NewGuid();

        _businessRepoMock
            .Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync((Business?)null);

        Func<Task> act = () => _service.GetBusinessBranchesAsync(businessId);

        await act.Should().ThrowAsync<BusinessNotFoundException>();
    }
    
    [Test]
    public async Task AddBranchesAsync_AddsBranch_WhenBusinessExists()
    {
        var businessId = Guid.NewGuid();
        var dto = new BranchDto(
            businessId,
            "Branch Name",
            "Street",
            "City",
            "State"
        );

        _businessRepoMock
            .Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(new Business { Id = businessId });
        _businessRepoMock
            .Setup(r => r.AddBusinessBranchAsync(It.IsAny<BusinessBranches>()))
            .Returns(Task.CompletedTask);
        _businessRepoMock
            .Setup(r => r.FindBranchByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new BusinessBranches());

        await _service.AddBranchesAsync(dto);

        _businessRepoMock.Verify(
            r => r.AddBusinessBranchAsync(It.Is<BusinessBranches>(
                b => b.BusinessId == businessId && b.BranchName == dto.BranchName
            )),
            Times.Once
        );
    }
    
    [Test]
    public async Task AddBranchesAsync_Throws_WhenBusinessNotFound()
    {
        var dto = new BranchDto(
            Guid.NewGuid(),
            "Branch",
            null,
            null,
            null
        );

        _businessRepoMock
            .Setup(r => r.FindByIdAsync(dto.BusinessId))
            .ReturnsAsync((Business?)null);

        Func<Task> act = () => _service.AddBranchesAsync(dto);

        await act.Should().ThrowAsync<BusinessNotFoundException>();
    }
    
    [Test]
    public async Task DeleteBranchesAsync_DeletesBranch_WhenFound()
    {
        var branchId = Guid.NewGuid();

        _businessRepoMock
            .Setup(r => r.FindBranchByIdAsync(branchId))
            .ReturnsAsync(new BusinessBranches { Id = branchId });
        _businessRepoMock
            .Setup(r => r.DeleteBusinessBranchAsync(branchId))
            .Returns(Task.CompletedTask);

        await _service.DeleteBranchesAsync(branchId);

        _businessRepoMock.Verify(
            r => r.DeleteBusinessBranchAsync(branchId),
            Times.Once
        );
    }
    
    [Test]
    public async Task DeleteBranchesAsync_Throws_WhenBranchNotFound()
    {
        var branchId = Guid.NewGuid();

        _businessRepoMock
            .Setup(r => r.FindBranchByIdAsync(branchId))
            .ReturnsAsync((BusinessBranches?)null);

        Func<Task> act = () => _service.DeleteBranchesAsync(branchId);

        await act.Should().ThrowAsync<BranchNotFoundException>();
    }
    
    [Test]
    public async Task UpdateBranchesAsync_UpdatesAndReturnsBranch_WhenFound()
    {
        var dto = new BranchUpdateDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Updated Name",
            "Street",
            "City",
            "State"
        );

        var branch = new BusinessBranches
        {
            Id = dto.Id,
            BranchName = "Old Name"
        };

        _businessRepoMock
            .Setup(r => r.FindBranchByIdAsync(dto.Id))
            .ReturnsAsync(branch);
        _businessRepoMock
            .Setup(r => r.UpdateBusinessBranchAsync(branch))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateBranchesAsync(dto);

        result.BranchName.Should().Be("Updated Name");
    }
    
    [Test]
    public async Task UpdateBranchesAsync_Throws_WhenBranchNotFound()
    {
        var dto = new BranchUpdateDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Name",
            null,
            null,
            null
        );

        _businessRepoMock
            .Setup(r => r.FindBranchByIdAsync(dto.Id))
            .ReturnsAsync((BusinessBranches?)null);

        Func<Task> act = () => _service.UpdateBranchesAsync(dto);

        await act.Should().ThrowAsync<BranchNotFoundException>();
    }
}