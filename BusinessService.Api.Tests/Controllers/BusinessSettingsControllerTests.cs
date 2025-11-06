using BusinessService.Api.Controllers;
using BusinessService.Application.DTOs.Settings;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BusinessService.Api.Tests.Controllers;

[TestFixture]
public class BusinessSettingsControllerTests
{
    private Mock<IBusinessSettingsService> _serviceMock = null!;
    private Mock<ILogger<BusinessSettingsController>> _loggerMock = null!;
    private BusinessSettingsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _serviceMock = new Mock<IBusinessSettingsService>();
        _loggerMock = new Mock<ILogger<BusinessSettingsController>>();
        _controller = new BusinessSettingsController(_serviceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task GetBusinessSettings_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var dto = new BusinessSettingsDto(
            Guid.NewGuid(),
            businessId,
            false,
            false,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null
        );

        _serviceMock.Setup(s => s.GetBusinessSettingsAsync(businessId))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetBusinessSettings(businessId);

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Test]
    public async Task UpdateBusinessSettings_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new UpdateBusinessSettingsRequest
        {
            ReviewsPrivate = false
        };

        var dto = new BusinessSettingsDto(
            Guid.NewGuid(),
            businessId,
            false,
            false,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            userId // ModifiedByUserId should be userId from endpoint
        );

        _serviceMock.Setup(s => s.UpdateBusinessSettingsAsync(businessId, request, userId))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.UpdateBusinessSettings(businessId, request, userId);

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(dto);
        
        // Verify the service was called with currentUserId
        _serviceMock.Verify(s => s.UpdateBusinessSettingsAsync(businessId, request, userId), Times.Once);
    }

    [Test]
    public async Task UpdateBusinessSettings_ShouldReturnBadRequest_WhenCurrentUserIdMissing()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var request = new UpdateBusinessSettingsRequest { ReviewsPrivate = false };

        // Act
        var result = await _controller.UpdateBusinessSettings(businessId, request, null);

        // Assert
        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task GetRepSettings_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();
        var dto = new BusinessRepSettingsDto(
            Guid.NewGuid(),
            businessRepId,
            new NotificationPreferencesDto(true, false, true),
            false,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null
        );

        _serviceMock.Setup(s => s.GetRepSettingsAsync(businessRepId))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetRepSettings(businessRepId);

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task UpdateRepSettings_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new UpdateRepSettingsRequest { DarkMode = true };

        var dto = new BusinessRepSettingsDto(
            Guid.NewGuid(),
            businessRepId,
            null,
            true,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            userId // ModifiedByUserId should be userId from endpoint
        );

        _serviceMock.Setup(s => s.UpdateRepSettingsAsync(businessRepId, request, userId))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.UpdateRepSettings(businessRepId, request, userId);

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeEquivalentTo(dto);
        
        // Verify the service was called with currentUserId
        _serviceMock.Verify(s => s.UpdateRepSettingsAsync(businessRepId, request, userId), Times.Once);
    }

    [Test]
    public async Task UpdateRepSettings_ShouldReturnBadRequest_WhenCurrentUserIdMissing()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();
        var request = new UpdateRepSettingsRequest { DarkMode = true };

        // Act
        var result = await _controller.UpdateRepSettings(businessRepId, request, null);

        // Assert
        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task ExtendDndMode_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var additionalHours = 24;
        var dto = new BusinessSettingsDto(
            Guid.NewGuid(),
            businessId,
            false,
            true,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(84),
            DateTime.UtcNow,
            DateTime.UtcNow,
            userId // ModifiedByUserId should be userId from endpoint
        );

        _serviceMock.Setup(s => s.ExtendDndModeAsync(businessId, additionalHours, userId))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.ExtendDndMode(businessId, additionalHours, userId);

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        
        // Verify the service was called with currentUserId
        _serviceMock.Verify(s => s.ExtendDndModeAsync(businessId, additionalHours, userId), Times.Once);
    }

    [Test]
    public async Task ExtendDndMode_ShouldReturnBadRequest_WhenHoursInvalid()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invalidHours = 0;

        // Act
        var result = await _controller.ExtendDndMode(businessId, invalidHours, userId);

        // Assert
        var badRequest = result as ObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task ExtendDndMode_ShouldReturnBadRequest_WhenCurrentUserIdMissing()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var additionalHours = 24;

        // Act
        var result = await _controller.ExtendDndMode(businessId, additionalHours, null);

        // Assert
        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
    }
}