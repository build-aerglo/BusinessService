using BusinessService.Application.DTOs;
using BusinessService.Application.Services;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace BusinessService.Application.Tests.Services;

[TestFixture]
public class TagServiceTests
{
    private Mock<ITagRepository> _repositoryMock = null!;
    private Mock<ICategoryRepository> _categoryRepoMock = null!;
    private TagService _service = null!;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<ITagRepository>();
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _service = new TagService(_categoryRepoMock.Object, _repositoryMock.Object);
    }
    
    [Test]
    public async Task CreateTagAsync_ShouldCreateTags_WhenCategoryExists_AndTagsDoNotExist()
    {
        var request = new NewTagRequest(
            CategoryId: Guid.NewGuid(),
            TagNames: new List<string> { "Food", "Drinks" }
        );

        _categoryRepoMock.Setup(r => r.ExistsAsync(request.CategoryId))
            .ReturnsAsync(true);

        _repositoryMock.SetupSequence(r => r.TagExistAsync(It.IsAny<string>()))
            .ReturnsAsync(false) // Food does not exist
            .ReturnsAsync(false); // Drinks does not exist

        _repositoryMock.Setup(r => r.AddTagsAsync(It.IsAny<Tags>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateTagAsync(request);

        result.Should().BeTrue();

        // Verify both tags were created
        _repositoryMock.Verify(r => r.AddTagsAsync(It.Is<Tags>(t => t.Name == "Food")), Times.Once);
        _repositoryMock.Verify(r => r.AddTagsAsync(It.Is<Tags>(t => t.Name == "Drinks")), Times.Once);
    }
    
    [Test]
    public async Task CreateTagAsync_ShouldSkipExistingTags()
    {
        var request = new NewTagRequest(
            CategoryId: Guid.NewGuid(),
            TagNames: new List<string> { "Food", "Drinks" }
        );

        _categoryRepoMock.Setup(r => r.ExistsAsync(request.CategoryId))
            .ReturnsAsync(true);

        _repositoryMock.SetupSequence(r => r.TagExistAsync(It.IsAny<string>()))
            .ReturnsAsync(true)  // Food exists → skip
            .ReturnsAsync(false); // Drinks does not exist → create

        _repositoryMock.Setup(r => r.AddTagsAsync(It.IsAny<Tags>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateTagAsync(request);

        result.Should().BeTrue();

        // Should NOT create Food
        _repositoryMock.Verify(
            r => r.AddTagsAsync(It.Is<Tags>(t => t.Name == "Food")),
            Times.Never);

        // Should create Drinks
        _repositoryMock.Verify(
            r => r.AddTagsAsync(It.Is<Tags>(t => t.Name == "Drinks")),
            Times.Once);
    }
    
    [Test]
    public async Task CreateTagAsync_ShouldThrow_WhenCategoryDoesNotExist()
    {
        var request = new NewTagRequest(
            CategoryId: Guid.NewGuid(),
            TagNames: new List<string> { "Food" }
        );

        _categoryRepoMock.Setup(r => r.ExistsAsync(request.CategoryId))
            .ReturnsAsync(false);

        Func<Task> act = async () => await _service.CreateTagAsync(request);

        await act.Should()
            .ThrowAsync<CategoryNotFoundException>()
            .WithMessage("Category does not exist.");

        _repositoryMock.Verify(r => r.AddTagsAsync(It.IsAny<Tags>()), Times.Never);
    }
    
    [Test]
    public async Task GetCategoryTagsAsync_ShouldReturnTags_WhenCategoryExists()
    {
        var categoryId = Guid.NewGuid();
        var expectedTags = new List<string> { "Food", "Snacks" };

        _categoryRepoMock.Setup(r => r.ExistsAsync(categoryId))
            .ReturnsAsync(true);

        _repositoryMock.Setup(r => r.GetTagsAsync(categoryId))
            .ReturnsAsync(expectedTags);

        var result = await _service.GetCategoryTagsAsync(categoryId);

        result.Should().BeEquivalentTo(expectedTags);
    }
    
    [Test]
    public async Task GetCategoryTagsAsync_ShouldThrow_WhenCategoryDoesNotExist()
    {
        var categoryId = Guid.NewGuid();

        _categoryRepoMock.Setup(r => r.ExistsAsync(categoryId))
            .ReturnsAsync(false);

        Func<Task> act = async () => await _service.GetCategoryTagsAsync(categoryId);

        await act.Should()
            .ThrowAsync<CategoryNotFoundException>()
            .WithMessage("Category does not exist.");

        _repositoryMock.Verify(r => r.GetTagsAsync(It.IsAny<Guid>()), Times.Never);
    }


}