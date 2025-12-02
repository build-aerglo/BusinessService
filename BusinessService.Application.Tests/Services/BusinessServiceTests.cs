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
        _service = new BusinessService.Application.Services.BusinessService(_businessRepoMock.Object, _categoryRepoMock.Object);
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
    public async Task UpdateBusinessAsync_ShouldUpdateBusiness_WhenValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };
        var business = new Business { Id = id, Name = "Old Name" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id))
                         .ReturnsAsync(business);

        // Act
        var result = await _service.UpdateBusinessAsync(id, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");

        _businessRepoMock.Verify(r => r.UpdateProfileAsync(It.Is<Business>(b => b.Name == "Updated Name")), Times.Once);
    }

    [Test]
    public void UpdateBusinessAsync_ShouldThrow_WhenBusinessNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id))
                         .ReturnsAsync((Business?)null);

        // Act
        Func<Task> act = async () => await _service.UpdateBusinessAsync(id, request);

        // Assert
        act.Should().ThrowAsync<BusinessNotFoundException>()
            .WithMessage($"Business {id} not found.");
    }
}
