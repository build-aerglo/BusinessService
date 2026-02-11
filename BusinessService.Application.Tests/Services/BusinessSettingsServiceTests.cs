using BusinessService.Application.DTOs.Settings;
using BusinessService.Application.Interfaces;
using BusinessService.Application.Services;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace BusinessService.Application.Tests.Services;

[TestFixture]
public class BusinessSettingsServiceTests
{
    private Mock<IBusinessSettingsRepository> _settingsRepoMock = null!;
    private Mock<IBusinessRepository> _businessRepoMock = null!;
    private Mock<IBusinessRepServiceClient> _businessRepClientMock = null!;
    private Mock<IUserServiceClient> _userServiceClientMock = null!;
    private Mock<IBusinessAutoResponseRepository> _autoResponseRepoMock = null!;
    private BusinessSettingsService _service = null!;

    [SetUp]
    public void Setup()
    {
        _settingsRepoMock = new Mock<IBusinessSettingsRepository>();
        _businessRepoMock = new Mock<IBusinessRepository>();
        _businessRepClientMock = new Mock<IBusinessRepServiceClient>();
        _userServiceClientMock = new Mock<IUserServiceClient>();
        _autoResponseRepoMock = new Mock<IBusinessAutoResponseRepository>();
        _service = new BusinessSettingsService(
            _settingsRepoMock.Object,
            _businessRepoMock.Object,
            _businessRepClientMock.Object,
            _userServiceClientMock.Object,
            _autoResponseRepoMock.Object
        );
    }

    // ========== Business Settings Tests ==========

    [Test]
    public async Task GetBusinessSettingsAsync_ShouldReturnSettings_WhenExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ReviewsPrivate = true,
            DndModeEnabled = false
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _settingsRepoMock.Setup(r => r.FindBusinessSettingsByBusinessIdAsync(businessId))
            .ReturnsAsync(settings);

        // Act
        var result = await _service.GetBusinessSettingsAsync(businessId);

        // Assert
        result.Should().NotBeNull();
        result.BusinessId.Should().Be(businessId);
        result.ReviewsPrivate.Should().BeTrue();
    }

    [Test]
    public async Task UpdateBusinessSettingsAsync_ShouldUpdateReviewsPrivate_WhenAuthorized()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var businessRepDto = new BusinessRepDto(Guid.NewGuid(), businessId, userId, null, null, DateTime.UtcNow);
        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ReviewsPrivate = false
        };

        var request = new UpdateBusinessSettingsRequest { ReviewsPrivate = true };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _businessRepClientMock.Setup(c => c.GetParentRepByBusinessIdAsync(businessId))
            .ReturnsAsync(businessRepDto);
        _settingsRepoMock.Setup(r => r.FindBusinessSettingsByBusinessIdAsync(businessId))
            .ReturnsAsync(settings);
        _settingsRepoMock.Setup(r => r.UpdateBusinessSettingsAsync(It.IsAny<BusinessSettings>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateBusinessSettingsAsync(businessId, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.ReviewsPrivate.Should().BeTrue();
        result.ModifiedByUserId.Should().Be(userId); // Verify currentUserId is used
    }

    [Test]
    public void UpdateBusinessSettingsAsync_ShouldThrow_WhenUnauthorized()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var wrongUserId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var businessRepDto = new BusinessRepDto(Guid.NewGuid(), businessId, userId, null, null, DateTime.UtcNow);

        var request = new UpdateBusinessSettingsRequest { ReviewsPrivate = true };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _businessRepClientMock.Setup(c => c.GetParentRepByBusinessIdAsync(businessId))
            .ReturnsAsync(businessRepDto);

        // Act
        Func<Task> act = async () => await _service.UpdateBusinessSettingsAsync(
            businessId, request, wrongUserId);

        // Assert
        act.Should().ThrowAsync<UnauthorizedSettingsAccessException>();
    }

    // ========== Rep Settings Tests ==========

    [Test]
    public async Task GetRepSettingsAsync_ShouldReturnSettings_WhenExists()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();
        var businessRepDto = new BusinessRepDto(businessRepId, Guid.NewGuid(), Guid.NewGuid(), null, null, DateTime.UtcNow);
        var settings = new BusinessRepSettings
        {
            Id = Guid.NewGuid(),
            BusinessRepId = businessRepId,
            DarkMode = true
        };

        _businessRepClientMock.Setup(c => c.GetBusinessRepByIdAsync(businessRepId))
            .ReturnsAsync(businessRepDto);
        _settingsRepoMock.Setup(r => r.FindRepSettingsByRepIdAsync(businessRepId))
            .ReturnsAsync(settings);

        // Act
        var result = await _service.GetRepSettingsAsync(businessRepId);

        // Assert
        result.Should().NotBeNull();
        result.BusinessRepId.Should().Be(businessRepId);
        result.DarkMode.Should().BeTrue();
    }

    [Test]
    public async Task UpdateRepSettingsAsync_ShouldUpdateDarkMode_WhenAuthorized()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var businessRepDto = new BusinessRepDto(businessRepId, Guid.NewGuid(), userId, null, null, DateTime.UtcNow);
        var settings = new BusinessRepSettings
        {
            Id = Guid.NewGuid(),
            BusinessRepId = businessRepId,
            DarkMode = false
        };

        var request = new UpdateRepSettingsRequest { DarkMode = true };

        _businessRepClientMock.Setup(c => c.GetBusinessRepByIdAsync(businessRepId))
            .ReturnsAsync(businessRepDto);
        _settingsRepoMock.Setup(r => r.FindRepSettingsByRepIdAsync(businessRepId))
            .ReturnsAsync(settings);
        _settingsRepoMock.Setup(r => r.UpdateRepSettingsAsync(It.IsAny<BusinessRepSettings>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateRepSettingsAsync(businessRepId, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.DarkMode.Should().BeTrue();
        result.ModifiedByUserId.Should().Be(userId); // Verify currentUserId is used
    }

    [Test]
    public void UpdateRepSettingsAsync_ShouldThrow_WhenUnauthorized()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var wrongUserId = Guid.NewGuid();
        var businessRepDto = new BusinessRepDto(businessRepId, Guid.NewGuid(), userId, null, null, DateTime.UtcNow);

        var request = new UpdateRepSettingsRequest { DarkMode = true };

        _businessRepClientMock.Setup(c => c.GetBusinessRepByIdAsync(businessRepId))
            .ReturnsAsync(businessRepDto);

        // Act
        Func<Task> act = async () => await _service.UpdateRepSettingsAsync(
            businessRepId, request, wrongUserId);

        // Assert
        act.Should().ThrowAsync<UnauthorizedSettingsAccessException>();
    }

    // ========== ExtendDndMode Tests ==========

    [Test]
    public async Task ExtendDndModeAsync_ShouldExtend_WhenUserIsSupportUser()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var supportUserId = Guid.NewGuid();
        var additionalHours = 24;

        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            DndModeEnabled = true,
            DndModeEnabledAt = DateTime.UtcNow.AddHours(-12),
            DndModeExpiresAt = DateTime.UtcNow.AddHours(48)
        };

        _settingsRepoMock.Setup(r => r.FindBusinessSettingsByBusinessIdAsync(businessId))
            .ReturnsAsync(settings);
        _userServiceClientMock.Setup(c => c.IsSupportUserAsync(supportUserId))
            .ReturnsAsync(true);
        _settingsRepoMock.Setup(r => r.UpdateBusinessSettingsAsync(It.IsAny<BusinessSettings>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ExtendDndModeAsync(businessId, additionalHours, supportUserId);

        // Assert
        result.Should().NotBeNull();
        result.DndModeEnabled.Should().BeTrue();
        result.ModifiedByUserId.Should().Be(supportUserId);
    }

    [Test]
    public void ExtendDndModeAsync_ShouldThrow_WhenUserIsNotSupportUser()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var regularUserId = Guid.NewGuid();
        var additionalHours = 24;

        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            DndModeEnabled = true,
            DndModeExpiresAt = DateTime.UtcNow.AddHours(48)
        };

        _settingsRepoMock.Setup(r => r.FindBusinessSettingsByBusinessIdAsync(businessId))
            .ReturnsAsync(settings);
        _userServiceClientMock.Setup(c => c.IsSupportUserAsync(regularUserId))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _service.ExtendDndModeAsync(
            businessId, additionalHours, regularUserId);

        // Assert
        act.Should().ThrowAsync<UnauthorizedSettingsAccessException>()
            .WithMessage("Only support users can extend DnD mode.");
    }

    // ========== PreferredModeOfContact Tests ==========

    [Test]
    public async Task UpdateBusinessSettingsAsync_ShouldUpdatePreferredContactMethod_WhenProvided()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var businessRepDto = new BusinessRepDto(Guid.NewGuid(), businessId, userId, null, null, DateTime.UtcNow);
        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId
        };

        var request = new UpdateBusinessSettingsRequest { PreferredModeOfContact = "email" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _businessRepClientMock.Setup(c => c.GetParentRepByBusinessIdAsync(businessId))
            .ReturnsAsync(businessRepDto);
        _settingsRepoMock.Setup(r => r.FindBusinessSettingsByBusinessIdAsync(businessId))
            .ReturnsAsync(settings);
        _settingsRepoMock.Setup(r => r.UpdateBusinessSettingsAsync(It.IsAny<BusinessSettings>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateBusinessSettingsAsync(businessId, request, userId);

        // Assert
        _businessRepoMock.Verify(r => r.UpdatePreferredContactMethodAsync(businessId, "email"), Times.Once);
    }

    [Test]
    public async Task UpdateBusinessSettingsAsync_ShouldNotUpdatePreferredContactMethod_WhenNotProvided()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var businessRepDto = new BusinessRepDto(Guid.NewGuid(), businessId, userId, null, null, DateTime.UtcNow);
        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId
        };

        var request = new UpdateBusinessSettingsRequest { ReviewsPrivate = true };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _businessRepClientMock.Setup(c => c.GetParentRepByBusinessIdAsync(businessId))
            .ReturnsAsync(businessRepDto);
        _settingsRepoMock.Setup(r => r.FindBusinessSettingsByBusinessIdAsync(businessId))
            .ReturnsAsync(settings);
        _settingsRepoMock.Setup(r => r.UpdateBusinessSettingsAsync(It.IsAny<BusinessSettings>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateBusinessSettingsAsync(businessId, request, userId);

        // Assert
        _businessRepoMock.Verify(r => r.UpdatePreferredContactMethodAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    // ========== AutoResponseEnabled -> allow_auto_response Tests ==========

    [Test]
    public async Task UpdateBusinessSettingsAsync_ShouldUpdateAllowAutoResponse_WhenAutoResponseEnabledProvided()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var businessRepDto = new BusinessRepDto(Guid.NewGuid(), businessId, userId, null, null, DateTime.UtcNow);
        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId
        };
        var autoResponse = new BusinessAutoResponse
        {
            BusinessId = businessId,
            AllowAutoResponse = false
        };

        var request = new UpdateBusinessSettingsRequest { AutoResponseEnabled = true };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _businessRepClientMock.Setup(c => c.GetParentRepByBusinessIdAsync(businessId))
            .ReturnsAsync(businessRepDto);
        _settingsRepoMock.Setup(r => r.FindBusinessSettingsByBusinessIdAsync(businessId))
            .ReturnsAsync(settings);
        _settingsRepoMock.Setup(r => r.UpdateBusinessSettingsAsync(It.IsAny<BusinessSettings>()))
            .Returns(Task.CompletedTask);
        _autoResponseRepoMock.Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync(autoResponse);

        // Act
        await _service.UpdateBusinessSettingsAsync(businessId, request, userId);

        // Assert
        _autoResponseRepoMock.Verify(r => r.UpdateAsync(It.Is<BusinessAutoResponse>(
            a => a.AllowAutoResponse == true
        )), Times.Once);
    }

    [Test]
    public async Task UpdateBusinessSettingsAsync_ShouldNotUpdateAllowAutoResponse_WhenAutoResponseEnabledNotProvided()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var businessRepDto = new BusinessRepDto(Guid.NewGuid(), businessId, userId, null, null, DateTime.UtcNow);
        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId
        };

        var request = new UpdateBusinessSettingsRequest { ReviewsPrivate = true };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _businessRepClientMock.Setup(c => c.GetParentRepByBusinessIdAsync(businessId))
            .ReturnsAsync(businessRepDto);
        _settingsRepoMock.Setup(r => r.FindBusinessSettingsByBusinessIdAsync(businessId))
            .ReturnsAsync(settings);
        _settingsRepoMock.Setup(r => r.UpdateBusinessSettingsAsync(It.IsAny<BusinessSettings>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateBusinessSettingsAsync(businessId, request, userId);

        // Assert
        _autoResponseRepoMock.Verify(r => r.FindByBusinessIdAsync(It.IsAny<Guid>()), Times.Never);
        _autoResponseRepoMock.Verify(r => r.UpdateAsync(It.IsAny<BusinessAutoResponse>()), Times.Never);
    }

    // ========== CurrentUserId from users table join Tests ==========

    [Test]
    public async Task GetBusinessSettingsAsync_ShouldReturnCurrentUserId_WhenBusinessUserExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var expectedUserId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ReviewsPrivate = false,
            DndModeEnabled = false
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _settingsRepoMock.Setup(r => r.FindBusinessSettingsByBusinessIdAsync(businessId))
            .ReturnsAsync(settings);
        _businessRepoMock.Setup(r => r.GetBusinessUserIdByBusinessIdAsync(businessId))
            .ReturnsAsync(expectedUserId);

        // Act
        var result = await _service.GetBusinessSettingsAsync(businessId);

        // Assert
        result.Should().NotBeNull();
        result.CurrentUserId.Should().Be(expectedUserId);
        _businessRepoMock.Verify(r => r.GetBusinessUserIdByBusinessIdAsync(businessId), Times.Once);
    }

    [Test]
    public async Task GetBusinessSettingsAsync_ShouldReturnNullCurrentUserId_WhenNoBusinessUserExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ReviewsPrivate = false,
            DndModeEnabled = false
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _settingsRepoMock.Setup(r => r.FindBusinessSettingsByBusinessIdAsync(businessId))
            .ReturnsAsync(settings);
        _businessRepoMock.Setup(r => r.GetBusinessUserIdByBusinessIdAsync(businessId))
            .ReturnsAsync((Guid?)null);

        // Act
        var result = await _service.GetBusinessSettingsAsync(businessId);

        // Assert
        result.Should().NotBeNull();
        result.CurrentUserId.Should().BeNull();
    }

    [Test]
    public void ExtendDndModeAsync_ShouldThrow_WhenDndModeNotEnabled()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var supportUserId = Guid.NewGuid();

        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            DndModeEnabled = false
        };

        _settingsRepoMock.Setup(r => r.FindBusinessSettingsByBusinessIdAsync(businessId))
            .ReturnsAsync(settings);

        // Act
        Func<Task> act = async () => await _service.ExtendDndModeAsync(
            businessId, 24, supportUserId);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DnD mode is not currently enabled.");
    }
}