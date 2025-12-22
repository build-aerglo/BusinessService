using BusinessService.Application.DTOs;
using BusinessService.Application.Services;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace BusinessService.Application.Tests.Services;

[TestFixture]
public class CategoryServiceTests
{
    private Mock<ICategoryRepository> _repoMock = null!;
    private CategoryService _service = null!;

    [SetUp]
    public void Setup()
    {
        _repoMock = new Mock<ICategoryRepository>();
        _service = new CategoryService(_repoMock.Object);
    }

    [Test]
    public async Task CreateCategoryAsync_ShouldCreate_WhenValid()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "Coffee",
            Description = "All about coffee"
        };

        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name))
                 .ReturnsAsync(false);

        _repoMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
                 .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateCategoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Coffee");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
    }

    [Test]
    public void CreateCategoryAsync_ShouldThrow_WhenNameExists()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Coffee",
            Description = "Duplicate category"
        };

        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name))
                 .ReturnsAsync(true);

        Func<Task> act = async () => await _service.CreateCategoryAsync(request);

        act.Should().ThrowAsync<CategoryAlreadyExistsException>()
           .WithMessage("Category name 'Coffee' already exists.");
    }

    [Test]
    public void CreateCategoryAsync_ShouldThrow_WhenParentNotFound()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Espresso",
            ParentCategoryId = Guid.NewGuid()
        };

        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name))
                 .ReturnsAsync(false);

        _repoMock.Setup(r => r.FindByIdAsync(request.ParentCategoryId.Value))
                 .ReturnsAsync((Category?)null);

        Func<Task> act = async () => await _service.CreateCategoryAsync(request);

        act.Should().ThrowAsync<CategoryNotFoundException>()
           .WithMessage($"Parent category with ID {request.ParentCategoryId} not found.");
    }

    [Test]
    public async Task GetAllTopLevelCategoriesAsync_ShouldReturnList()
    {
        var categories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Food" },
            new() { Id = Guid.NewGuid(), Name = "Drinks" }
        };

        _repoMock.Setup(r => r.FindTopLevelAsync())
                 .ReturnsAsync(categories);

        var result = await _service.GetAllTopLevelCategoriesAsync();

        result.Should().HaveCount(2);
        result.Select(c => c.Name).Should().Contain(new[] { "Food", "Drinks" });
    }
    
    [Test]
    public async Task GetCategoryTags_ShouldReturnCategoryWithTags_WhenCategoryExists()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        _repoMock.Setup(r => r.FindByIdAsync(categoryId))
            .ReturnsAsync(new Category
            {
                Id = categoryId,
                Name = "Shopping"
            });

        _repoMock.Setup(r => r.GetTagsByCategoryIdAsync(categoryId))
            .ReturnsAsync(new List<Tags>
            {
                new Tags { Id = Guid.NewGuid(), CategoryId = categoryId, Name = "electronics" },
                new Tags { Id = Guid.NewGuid(), CategoryId = categoryId, Name = "clothing" }
            });

        // Act
        var result = await _service.GetCategoryTagsAsync(categoryId);

        // Assert
        result.CategoryName.Should().Be("Shopping");
        result.Tags.Should().HaveCount(2);
        result.Tags.Select(t => t.Name).Should().Contain(new [] { "electronics", "clothing" });
    }

    [Test]
    public void GetCategoryTags_ShouldThrowNotFound_WhenCategoryDoesNotExist()
    {
        var categoryId = Guid.NewGuid();

        _repoMock.Setup(r => r.FindByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        var act = async () => await _service.GetCategoryTagsAsync(categoryId);

        act.Should().ThrowAsync<CategoryNotFoundException>()
            .WithMessage("Category not found.");
    }
}
