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
public class BusinessControllerTests
{
    private Mock<IBusinessService> _serviceMock = null!;
    private Mock<ILogger<BusinessController>> _loggerMock = null!;
    private BusinessController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _serviceMock = new Mock<IBusinessService>();
        _loggerMock = new Mock<ILogger<BusinessController>>();
        _controller = new BusinessController(_serviceMock.Object, _loggerMock.Object);
    }

    // Helper to generate valid DTO
    private BusinessDto CreateDto(Guid? id = null, string name = "Test Business")
    {
        return new BusinessDto(
            id ?? Guid.NewGuid(),
            name,
            "https://example.com",
            false,
            0m,
            0,
            null,
            new List<CategoryDto>(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            null,
            null,
            0,
            new List<FaqDto>()
        );
    }

    [Test]
    public async Task CreateBusiness_ShouldReturnCreated_WhenSuccessful()
    {
        // Arrange
        var request = new CreateBusinessRequest
        {
            Name = "Test Business",
            Website = "https://example.com",
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        };

        var expectedDto = CreateDto(name: "Test Business");

        _serviceMock.Setup(s => s.CreateBusinessAsync(request))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.CreateBusiness(request);

        // Assert
        var created = result as CreatedAtActionResult;
        created.Should().NotBeNull();
        created!.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(expectedDto);
    }

    [Test]
    public async Task CreateBusiness_ShouldReturnConflict_WhenAlreadyExists()
    {
        // Arrange
        var request = new CreateBusinessRequest
        {
            Name = "Duplicate Business",
            Website = "https://duplicate.com",
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        };

        _serviceMock.Setup(s => s.CreateBusinessAsync(request))
            .ThrowsAsync(new BusinessAlreadyExistsException("Business already exists."));

        // Act
        var result = await _controller.CreateBusiness(request);

        // Assert
        var conflict = result as ObjectResult;
        conflict.Should().NotBeNull();
        conflict!.StatusCode.Should().Be(409);
        conflict.Value.Should().BeEquivalentTo(new { error = "Business already exists." });
    }

    [Test]
    public async Task GetBusiness_ShouldReturnOk_WhenFound()
    {
        var id = Guid.NewGuid();
        var dto = CreateDto(id, "Shop");

        _serviceMock.Setup(s => s.GetBusinessAsync(id)).ReturnsAsync(dto);

        var result = await _controller.GetBusiness(id);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeEquivalentTo(dto);
    }

    [Test]
    public async Task UpdateBusiness_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };
        var expectedDto = CreateDto(id, "Updated Name");

        _serviceMock.Setup(s => s.UpdateBusinessAsync(id, request))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.UpdateBusiness(id, request);

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expectedDto);
    }

    [Test]
    public async Task UpdateBusiness_ShouldReturnNotFound_WhenBusinessNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };

        _serviceMock.Setup(s => s.UpdateBusinessAsync(id, request))
            .ThrowsAsync(new BusinessNotFoundException("Business not found."));

        // Act
        var result = await _controller.UpdateBusiness(id, request);

        // Assert
        var notFound = result as ObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
        notFound.Value.Should().BeEquivalentTo(new { error = "Business not found." });
    }

    [Test]
    public async Task UpdateBusiness_ShouldReturnConflict_WhenBusinessConflict()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };

        _serviceMock.Setup(s => s.UpdateBusinessAsync(id, request))
            .ThrowsAsync(new BusinessConflictException("Business conflict."));

        // Act
        var result = await _controller.UpdateBusiness(id, request);

        // Assert
        var conflict = result as ObjectResult;
        conflict.Should().NotBeNull();
        conflict!.StatusCode.Should().Be(409);
        conflict.Value.Should().BeEquivalentTo(new { error = "Business conflict." });
    }
}
