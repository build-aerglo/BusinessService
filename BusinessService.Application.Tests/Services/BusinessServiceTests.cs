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
    private Mock<IBusinessVerificationRepository> _verificationRepoMock = null!;

    [SetUp]
    public void Setup()
    {
        _businessRepoMock = new Mock<IBusinessRepository>();
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _qrCodeServiceMock = new Mock<IQrCodeService>();
        _searchProducerMock = new Mock<IBusinessSearchProducer>();
        _tagRepoMock = new Mock<ITagRepository>();
        _verificationRepoMock = new Mock<IBusinessVerificationRepository>();

        // Always return a fixed Base64 encoded QR code for predictable tests
        _qrCodeServiceMock.Setup(q => q.GenerateQrCodeBase64(It.IsAny<string>()))
            .Returns("BASE64_QR_CODE_TEST_VALUE");

        _service = new Application.Services.BusinessService(
            _businessRepoMock.Object,
            _categoryRepoMock.Object,
            _qrCodeServiceMock.Object,
            _searchProducerMock.Object,
            _tagRepoMock.Object,
            _verificationRepoMock.Object
        );

    }

    // ---------------------------------------------------------
    // CREATE BUSINESS TESTS
    // ---------------------------------------------------------
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

        // Assertions
        result.Should().NotBeNull();
        result.Name.Should().Be("Alpha Coffee");
        result.Categories.Should().ContainSingle(c => c.Name == "Coffee");
        result.QrCodeBase64.Should().Be("BASE64_QR_CODE_TEST_VALUE");

        _businessRepoMock.Verify(r => r.AddAsync(It.IsAny<Business>()), Times.Once);
        _qrCodeServiceMock.Verify(q => q.GenerateQrCodeBase64(It.IsAny<string>()), Times.Once);

        // NEW: Ensure Search Service is notified
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

        // return only 1 category even though request has 2 → triggers error
        _categoryRepoMock.Setup(r => r.FindAllByIdsAsync(request.CategoryIds))
            .ReturnsAsync(new List<Category> { new Category { Id = request.CategoryIds[0] } });

        Func<Task> act = async () => await _service.CreateBusinessAsync(request);

        act.Should().ThrowAsync<CategoryNotFoundException>()
            .WithMessage("One or more categories not found.");
    }

    // ---------------------------------------------------------
    // UPDATE BUSINESS TESTS
    // ---------------------------------------------------------
    [Test]
    public async Task UpdateBusinessAsync_ShouldUpdateBusiness_WhenValid()
    {
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };
        var business = new Business { Id = id, Name = "Old Name" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(business);
        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        var result = await _service.UpdateBusinessAsync(id, request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");

        _businessRepoMock.Verify(r => r.UpdateProfileAsync(It.Is<Business>(b => b.Name == "Updated Name")), Times.Once);

        // QR code should NOT regenerate during update
        _qrCodeServiceMock.Verify(q => q.GenerateQrCodeBase64(It.IsAny<string>()), Times.Never);

        // NEW: Ensure Search Service update is called
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
                    Categories = new List<Category>()
                },
                new Business
                {
                    Id = Guid.NewGuid(),
                    Name = "Acme Repair Co",
                    AvgRating = 4.7m,
                    ReviewCount = 300,
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
    
    // business claim tests

    [Test]
    public async Task ClaimBusinessAsync_ShouldClaimBusiness_WhenBusinessExistsAndNotClaimed()
    {
        // Arrange
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

        // Act
        await _service.ClaimBusinessAsync(dto);

        // Assert
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
        // Arrange
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
            .ReturnsAsync((Business)null);

        // Act
        Func<Task> act = async () => await _service.ClaimBusinessAsync(dto);

        // Assert
        act.Should().ThrowAsync<BusinessNotFoundException>()
            .WithMessage($"Business {businessId} not found.");

        _businessRepoMock.Verify(r => r.ClaimAsync(It.IsAny<BusinessClaims>()), Times.Never);
    }

    [Test]
    public void ClaimBusinessAsync_ShouldThrowBusinessConflictException_WhenBusinessIsApproved()
    {
        // Arrange
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

        // Act
        Func<Task> act = async () => await _service.ClaimBusinessAsync(dto);

        // Assert
        act.Should().ThrowAsync<BusinessConflictException>()
            .WithMessage("Business already approved.");

        _businessRepoMock.Verify(r => r.ClaimAsync(It.IsAny<BusinessClaims>()), Times.Never);
    }

    [Test]
    public void ClaimBusinessAsync_ShouldThrowBusinessConflictException_WhenBusinessIsInProgress()
    {
        // Arrange
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

        // Act
        Func<Task> act = async () => await _service.ClaimBusinessAsync(dto);

        // Assert
        act.Should().ThrowAsync<BusinessConflictException>()
            .WithMessage("Business approval in progress.");

        _businessRepoMock.Verify(r => r.ClaimAsync(It.IsAny<BusinessClaims>()), Times.Never);
    }
    
    // branches
    [Test]
    public async Task GetBusinessBranchesAsync_ReturnsBranches_WhenBusinessExists()
    {
        // Arrange
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

        // Act
        var result = await _service.GetBusinessBranchesAsync(businessId);

        // Assert
        result.Should().BeEquivalentTo(branches);
    }

    [Test]
    public async Task GetBusinessBranchesAsync_Throws_WhenBusinessNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();

        _businessRepoMock
            .Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync((Business?)null);

        // Act
        Func<Task> act = () => _service.GetBusinessBranchesAsync(businessId);

        // Assert
        await act.Should().ThrowAsync<BusinessNotFoundException>();
    }
    
    [Test]
    public async Task AddBranchesAsync_AddsBranch_WhenBusinessExists()
    {
        // Arrange
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

        // Act
        await _service.AddBranchesAsync(dto);

        // Assert
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
        // Arrange
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

        // Act
        Func<Task> act = () => _service.AddBranchesAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessNotFoundException>();
    }
    
    [Test]
    public async Task DeleteBranchesAsync_DeletesBranch_WhenFound()
    {
        // Arrange
        var branchId = Guid.NewGuid();

        _businessRepoMock
            .Setup(r => r.FindBranchByIdAsync(branchId))
            .ReturnsAsync(new BusinessBranches { Id = branchId });

        _businessRepoMock
            .Setup(r => r.DeleteBusinessBranchAsync(branchId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteBranchesAsync(branchId);

        // Assert
        _businessRepoMock.Verify(
            r => r.DeleteBusinessBranchAsync(branchId),
            Times.Once
        );
    }
    
    [Test]
    public async Task DeleteBranchesAsync_Throws_WhenBranchNotFound()
    {
        // Arrange
        var branchId = Guid.NewGuid();

        _businessRepoMock
            .Setup(r => r.FindBranchByIdAsync(branchId))
            .ReturnsAsync((BusinessBranches?)null);

        // Act
        Func<Task> act = () => _service.DeleteBranchesAsync(branchId);

        // Assert
        await act.Should().ThrowAsync<BranchNotFoundException>();
    }
    
    [Test]
    public async Task UpdateBranchesAsync_UpdatesAndReturnsBranch_WhenFound()
    {
        // Arrange
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

        // Act
        var result = await _service.UpdateBranchesAsync(dto);

        // Assert
        result.BranchName.Should().Be("Updated Name");
    }
    
    [Test]
    public async Task UpdateBranchesAsync_Throws_WhenBranchNotFound()
    {
        // Arrange
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

        // Act
        Func<Task> act = () => _service.UpdateBranchesAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BranchNotFoundException>();
    }

    // ---------------------------------------------------------
    // VERIFICATION INTEGRATION TESTS
    // ---------------------------------------------------------
    [Test]
    public async Task CreateBusinessAsync_ShouldCreateVerificationEntry_WhenBusinessIsCreated()
    {
        // Arrange
        var request = new CreateBusinessRequest
        {
            Name = "Verified Biz",
            Website = "https://verifiedbiz.com",
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        };

        var category = new Category { Id = request.CategoryIds[0], Name = "Tech" };

        _businessRepoMock.Setup(r => r.ExistsByNameAsync(request.Name)).ReturnsAsync(false);
        _categoryRepoMock.Setup(r => r.FindAllByIdsAsync(request.CategoryIds))
            .ReturnsAsync(new List<Category> { category });
        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        // Act
        var result = await _service.CreateBusinessAsync(request);

        // Assert
        result.Should().NotBeNull();
        _verificationRepoMock.Verify(r => r.AddAsync(It.Is<BusinessVerification>(
            v => v.BusinessId == result.Id
        )), Times.Once);
    }

    [Test]
    public async Task UpdateBusinessAsync_ShouldResetPhoneVerification_WhenPhoneNumberChanges()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { BusinessPhoneNumber = "+1234567890" };
        var business = new Business
        {
            Id = id,
            Name = "Test Biz",
            BusinessPhoneNumber = "+0987654321" // Different phone number
        };
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = id,
            PhoneVerified = true,
            EmailVerified = true
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(business);
        _verificationRepoMock.Setup(r => r.FindByBusinessIdAsync(id)).ReturnsAsync(verification);
        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        // Act
        await _service.UpdateBusinessAsync(id, request);

        // Assert
        _verificationRepoMock.Verify(r => r.UpdatePhoneAndEmailVerificationAsync(
            id,
            false,  // phoneVerified should be false
            true,   // emailVerified should remain true
            "+1234567890"
        ), Times.Once);
    }

    [Test]
    public async Task UpdateBusinessAsync_ShouldResetEmailVerification_WhenEmailChanges()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { BusinessEmail = "new@example.com" };
        var business = new Business
        {
            Id = id,
            Name = "Test Biz",
            BusinessEmail = "old@example.com" // Different email
        };
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = id,
            PhoneVerified = true,
            EmailVerified = true
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(business);
        _verificationRepoMock.Setup(r => r.FindByBusinessIdAsync(id)).ReturnsAsync(verification);
        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        // Act
        await _service.UpdateBusinessAsync(id, request);

        // Assert
        _verificationRepoMock.Verify(r => r.UpdatePhoneAndEmailVerificationAsync(
            id,
            true,   // phoneVerified should remain true
            false,  // emailVerified should be false
            null    // phone number not changed
        ), Times.Once);
    }

    [Test]
    public async Task UpdateBusinessAsync_ShouldNotResetVerification_WhenPhoneAndEmailUnchanged()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };
        var business = new Business { Id = id, Name = "Old Name" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(business);
        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        // Act
        await _service.UpdateBusinessAsync(id, request);

        // Assert
        _verificationRepoMock.Verify(r => r.UpdatePhoneAndEmailVerificationAsync(
            It.IsAny<Guid>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<string?>()
        ), Times.Never);
    }

    [Test]
    public async Task UpdateBusinessAsync_ShouldNotUpdateEmail_WhenEmailProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var originalEmail = "original@example.com";
        var request = new UpdateBusinessRequest { BusinessEmail = "new@example.com" };
        var business = new Business
        {
            Id = id,
            Name = "Test Biz",
            BusinessEmail = originalEmail
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(business);
        _tagRepoMock.Setup(r => r.FindByNamesAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Tags>());

        // Act
        await _service.UpdateBusinessAsync(id, request);

        // Assert - email should remain unchanged (email update is disabled)
        _businessRepoMock.Verify(r => r.UpdateProfileAsync(It.Is<Business>(
            b => b.BusinessEmail == originalEmail
        )), Times.Once);
    }

}

