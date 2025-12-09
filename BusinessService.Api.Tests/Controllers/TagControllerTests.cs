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
public class TagControllerTests
{
    
    private Mock<ITagService> _serviceMock = null!;
    private Mock<ILogger<TagController>> _loggerMock = null!;
    private TagController _controller = null!;
    
    [SetUp]
    public void Setup()
    {
        _serviceMock = new Mock<ITagService>();
        _loggerMock = new Mock<ILogger<TagController>>();
        _controller = new TagController(_serviceMock.Object, _loggerMock.Object);
    }
    
    [Test]
    public async Task CreateTag_ShouldReturnOk_WhenSuccessful()
    {
        var request = new NewTagRequest(
            CategoryId: Guid.NewGuid(),
            TagNames: new List<string> { "Fruits", "Snacks" }
        );

        // var expectedTags = new List<TagDto>
        // {
        //     new TagDto(Guid.NewGuid(), request.CategoryId, "Fruits"),
        //     new TagDto(Guid.NewGuid(), request.CategoryId, "Snacks")
        // };

        _serviceMock.Setup(s => s.CreateTagAsync(request))
            .ReturnsAsync(true);

        var result = await _controller.CreateTag(request);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(true);
    }


    [Test]
    public async Task CreateTag_ShouldReturnConflict_WhenCategoryNotFound()
    {
        var request = new NewTagRequest(
            CategoryId: Guid.NewGuid(),
            TagNames: new List<string> { "Fruits" }
        );

        _serviceMock.Setup(s => s.CreateTagAsync(request))
            .ThrowsAsync(new CategoryNotFoundException("Category does not exist."));

        var result = await _controller.CreateTag(request);

        var conflict = result as ObjectResult;
        conflict.Should().NotBeNull();
        conflict!.StatusCode.Should().Be(409);
        conflict.Value.Should().BeEquivalentTo(new { error = "Category does not exist." });
    }


    [Test]
    public async Task CreateTag_ShouldReturnBadRequest_WhenUnexpectedErrorOccurs()
    {
        var request = new NewTagRequest(
            CategoryId: Guid.NewGuid(),
            TagNames: new List<string> { "Fruits" }
        );

        _serviceMock.Setup(s => s.CreateTagAsync(request))
            .ThrowsAsync(new Exception("Unexpected error."));

        var result = await _controller.CreateTag(request);

        var badRequest = result as ObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
        badRequest.Value.Should().BeEquivalentTo(new { error = "Unexpected error." });
    }


}