using BusinessService.Application.DTOs;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace BusinessService.Application.Tests.Services;

[TestFixture]
public class BusinessServiceTests
{
    private Mock<IBusinessRepository> _businessRepoMock = null!;
    private Mock<ICategoryRepository> _categoryRepoMock = null!;
    private BusinessService.Application.Services.BusinessService _service = null!;

    [SetUp]
    public void Setup()
    {
        _businessRepoMock = new Mock<IBusinessRepository>();
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _service = new BusinessService.Application.Services.BusinessService(_businessRepoMock.Object,
            _categoryRepoMock.Object);
    }

    [Test]
    public async Task CreateBusinessAsync_ShouldCreateBusiness_WhenValid()
    {
        // Arrange
        var request = new CreateBusinessRequest
        {
            Name = "Alpha Coffee",
            Website = "https://alphacoffee.com",
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        };

        var category = new Category { Id = request.CategoryIds[0], Name = "Coffee" };

        _businessRepoMock.Setup(r => r.ExistsByNameAsync(request.Name))
            .ReturnsAsync(false);

        _categoryRepoMock.Setup(r => r.FindAllByIdsAsync(request.CategoryIds))
            .ReturnsAsync(new List<Category> { category });

        // Act
        var result = await _service.CreateBusinessAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Alpha Coffee");
        result.Categories.Should().ContainSingle(c => c.Name == "Coffee");

        _businessRepoMock.Verify(r => r.AddAsync(It.IsAny<Business>()), Times.Once);
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

        _businessRepoMock.Setup(r => r.ExistsByNameAsync(request.Name))
            .ReturnsAsync(true);

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

        _businessRepoMock.Setup(r => r.ExistsByNameAsync(request.Name))
            .ReturnsAsync(false);

        _categoryRepoMock.Setup(r => r.FindAllByIdsAsync(request.CategoryIds))
            .ReturnsAsync(new List<Category> { new Category { Id = request.CategoryIds[0] } });

        Func<Task> act = async () => await _service.CreateBusinessAsync(request);

        act.Should().ThrowAsync<CategoryNotFoundException>()
            .WithMessage("One or more categories not found.");
    }
    
    [Test]
    public void UpdateBusinessDetailsAsync_ShouldThrow_WhenBusinessNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var dto = new UpdateBusinessDto(
            "Nonexistent",
            "https://fake.com",
            new List<string>()
        );

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync((Business?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessNotFoundException>(async () =>
            await _service.UpdateBusinessDetailsAsync(businessId, dto)
        );

        Assert.That(ex!.Message, Does.Contain("not found"));
        _businessRepoMock.Verify(r => r.UpdateBusinessDetailsAsync(It.IsAny<Business>(), It.IsAny<List<string>?>()),
            Times.Never);
    }

    [Test]
    public async Task UpdateBusinessDetailsAsync_ShouldUpdateBusiness_WhenExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var initialCategoryId = Guid.NewGuid();
        var updatedCategoryId = Guid.NewGuid();

        // Create request and category for initial business
        var createRequest = new CreateBusinessRequest
        {
            Name = "Alpha Coffee",
            Website = "https://alphacoffee.com",
            CategoryIds = new List<Guid> { initialCategoryId }
        };

        var initialCategory = new Category { Id = initialCategoryId, Name = "Coffee" };

        _businessRepoMock.Setup(r => r.ExistsByNameAsync(createRequest.Name))
            .ReturnsAsync(false);

        _categoryRepoMock.Setup(r => r.FindAllByIdsAsync(createRequest.CategoryIds))
            .ReturnsAsync(new List<Category> { initialCategory });

        _businessRepoMock.Setup(r => r.AddAsync(It.IsAny<Business>()))
            .Returns(Task.CompletedTask);

        // Simulate created business
        var createdBusiness = new Business
        {
            Id = businessId,
            Name = createRequest.Name,
            Website = createRequest.Website,
            Categories = new List<Category> { initialCategory },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(createdBusiness);

        // Mock category repo for updated categories
        var updatedCategories = new List<Category> { new Category { Id = updatedCategoryId, Name = "Bakery" } };

        _categoryRepoMock.Setup(r => r.FindAllByIdsAsync(It.Is<List<Guid>>(ids => ids.Contains(updatedCategoryId))))
            .ReturnsAsync(updatedCategories);

        // Act
        var updateDto = new UpdateBusinessDto(
            "Beta Coffee",
            "https://betacoffee.com",
            new List<string> { updatedCategoryId.ToString() }
        );

        await _service.UpdateBusinessDetailsAsync(businessId, updateDto);

        // Assert
        _businessRepoMock.Verify(r => r.UpdateBusinessDetailsAsync(
            It.Is<Business>(b =>
                b.Name == updateDto.Name &&
                b.Website == updateDto.Website &&
                b.Categories.Any(c => c.Id == updatedCategoryId) &&
                b.UpdatedAt > DateTime.UtcNow.AddSeconds(-5)
            ),
            It.Is<List<string>?>(cats =>
                cats != null && cats.Count == 1 && cats.First() == updatedCategoryId.ToString())
        ), Times.Once);
    }

}
