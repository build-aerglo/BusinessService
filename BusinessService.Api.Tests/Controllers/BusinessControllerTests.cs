﻿using BusinessService.Api.Controllers;
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

        var expectedDto = new BusinessDto(
            Guid.NewGuid(), "Test Business", "https://example.com", false, 0, 0, null, new List<CategoryDto>()
        );

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
        var dto = new BusinessDto(id, "Shop", null, false, 4.2m, 12, null, new List<CategoryDto>());
        _serviceMock.Setup(s => s.GetBusinessAsync(id)).ReturnsAsync(dto);

        var result = await _controller.GetBusiness(id);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeEquivalentTo(dto);
    }
}
