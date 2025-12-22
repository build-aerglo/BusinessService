using BusinessService.Api.Controllers;
using BusinessService.Application.DTOs;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BusinessService.Api.Tests.Controllers;

[TestFixture]
public class CategoryControllerTests
{
    private Mock<ICategoryService> _serviceMock = null!;
    private Mock<ILogger<CategoryController>> _loggerMock = null!;
    private CategoryController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _serviceMock = new Mock<ICategoryService>();
        _loggerMock = new Mock<ILogger<CategoryController>>();
        _controller = new CategoryController(_serviceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task CreateCategory_ShouldReturnCreated_WhenSuccessful()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Food",
            Description = "Restaurants and dining"
        };

        var expectedDto = new CategoryDto(Guid.NewGuid(), "Food", "Restaurants and dining", null);
        _serviceMock.Setup(s => s.CreateCategoryAsync(request))
                    .ReturnsAsync(expectedDto);

        var result = await _controller.CreateCategory(request);

        var created = result as CreatedAtActionResult;
        created.Should().NotBeNull();
        created!.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(expectedDto);
    }

    [Test]
    public async Task CreateCategory_ShouldReturnConflict_WhenDuplicate()
    {
        var request = new CreateCategoryRequest { Name = "Food" };
        _serviceMock.Setup(s => s.CreateCategoryAsync(request))
                    .ThrowsAsync(new CategoryAlreadyExistsException("Category already exists."));

        var result = await _controller.CreateCategory(request);

        var conflict = result as ObjectResult;
        conflict.Should().NotBeNull();
        conflict!.StatusCode.Should().Be(409);
        conflict.Value.Should().BeEquivalentTo(new { error = "Category already exists." });
    }

    [Test]
    public async Task GetCategory_ShouldReturnOk_WhenFound()
    {
        var id = Guid.NewGuid();
        var dto = new CategoryDto(id, "Food", null, null);
        _serviceMock.Setup(s => s.GetCategoryAsync(id)).ReturnsAsync(dto);

        var result = await _controller.GetCategory(id);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Test]
    public async Task GetCategory_ShouldReturnNotFound_WhenMissing()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetCategoryAsync(id))
                    .ThrowsAsync(new CategoryNotFoundException($"Category {id} not found."));

        var result = await _controller.GetCategory(id);

        var notFound = result as ObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task GetTopLevelCategories_ShouldReturnOk()
    {
        var list = new List<CategoryDto>
        {
            new(Guid.NewGuid(), "Food", null, null),
            new(Guid.NewGuid(), "Drinks", null, null)
        };

        _serviceMock.Setup(s => s.GetAllTopLevelCategoriesAsync())
                    .ReturnsAsync(list);

        var result = await _controller.GetTopLevelCategories();

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(list);
    }
    
    [Test]
    public async Task GetAllCategories_ShouldReturnOk_WithCategoryList()
    {
        // Arrange
        var expected = new List<CategoryDto>
        {
            new CategoryDto(Guid.NewGuid(), "Shopping", "All shopping stores", null),
            new CategoryDto(Guid.NewGuid(), "Food", "Restaurants and cafes", null)
        };

        _serviceMock
            .Setup(s => s.GetAllCategoriesAsync())
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetAllCategories();

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expected);
    }
    [Test]
    public async Task GetAllCategories_ShouldReturnOk_WithEmptyList_WhenNoCategories()
    {
        _serviceMock
            .Setup(s => s.GetAllCategoriesAsync())
            .ReturnsAsync(new List<CategoryDto>());

        var result = await _controller.GetAllCategories();

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ((List<CategoryDto>)ok.Value!).Should().BeEmpty();
    }

}
