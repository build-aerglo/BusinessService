using BusinessService.Application.DTOs.AutoResponse;
using BusinessService.Application.Services;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using Moq;
using NUnit.Framework;

namespace BusinessService.Application.Tests.Services;

[TestFixture]
public class BusinessAutoResponseServiceTests
{
    private Mock<IBusinessAutoResponseRepository> _autoResponseRepositoryMock = null!;
    private Mock<IBusinessRepository> _businessRepositoryMock = null!;
    private BusinessAutoResponseService _service = null!;

    [SetUp]
    public void Setup()
    {
        _autoResponseRepositoryMock = new Mock<IBusinessAutoResponseRepository>();
        _businessRepositoryMock = new Mock<IBusinessRepository>();
        _service = new BusinessAutoResponseService(
            _autoResponseRepositoryMock.Object,
            _businessRepositoryMock.Object);
    }

    // ========== GetByBusinessIdAsync Tests ==========

    [Test]
    public async Task GetByBusinessIdAsync_ShouldReturnDto_WhenExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var autoResponse = new BusinessAutoResponse
        {
            BusinessId = businessId,
            PositiveResponse = "Thank you!",
            NegativeResponse = "Sorry!",
            NeutralResponse = "Thanks for feedback!",
            AllowAutoResponse = true
        };

        _autoResponseRepositoryMock
            .Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync(autoResponse);

        // Act
        var result = await _service.GetByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.BusinessId, Is.EqualTo(businessId));
        Assert.That(result.PositiveResponse, Is.EqualTo("Thank you!"));
        Assert.That(result.NegativeResponse, Is.EqualTo("Sorry!"));
        Assert.That(result.NeutralResponse, Is.EqualTo("Thanks for feedback!"));
        Assert.That(result.AllowAutoResponse, Is.True);
    }

    [Test]
    public async Task GetByBusinessIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();

        _autoResponseRepositoryMock
            .Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync((BusinessAutoResponse?)null);

        // Act
        var result = await _service.GetByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Null);
    }

    // ========== UpdateAsync Tests ==========

    [Test]
    public async Task UpdateAsync_ShouldUpdateAllFields()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var existingAutoResponse = new BusinessAutoResponse
        {
            BusinessId = businessId,
            PositiveResponse = null,
            NegativeResponse = null,
            NeutralResponse = null,
            AllowAutoResponse = false
        };

        var request = new UpdateBusinessAutoResponseRequest
        {
            PositiveResponse = "New positive",
            NegativeResponse = "New negative",
            NeutralResponse = "New neutral",
            AllowAutoResponse = true
        };

        _businessRepositoryMock
            .Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(business);

        _autoResponseRepositoryMock
            .Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync(existingAutoResponse);

        // Act
        var result = await _service.UpdateAsync(businessId, request);

        // Assert
        Assert.That(result.PositiveResponse, Is.EqualTo("New positive"));
        Assert.That(result.NegativeResponse, Is.EqualTo("New negative"));
        Assert.That(result.NeutralResponse, Is.EqualTo("New neutral"));
        Assert.That(result.AllowAutoResponse, Is.True);

        _autoResponseRepositoryMock.Verify(r => r.UpdateAsync(It.Is<BusinessAutoResponse>(
            ar => ar.PositiveResponse == "New positive" &&
                  ar.NegativeResponse == "New negative" &&
                  ar.NeutralResponse == "New neutral" &&
                  ar.AllowAutoResponse == true
        )), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var existingAutoResponse = new BusinessAutoResponse
        {
            BusinessId = businessId,
            PositiveResponse = "Existing positive",
            NegativeResponse = "Existing negative",
            NeutralResponse = "Existing neutral",
            AllowAutoResponse = false
        };

        var request = new UpdateBusinessAutoResponseRequest
        {
            AllowAutoResponse = true
            // Other fields are null, should not be updated
        };

        _businessRepositoryMock
            .Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(business);

        _autoResponseRepositoryMock
            .Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync(existingAutoResponse);

        // Act
        var result = await _service.UpdateAsync(businessId, request);

        // Assert
        Assert.That(result.PositiveResponse, Is.EqualTo("Existing positive"));
        Assert.That(result.NegativeResponse, Is.EqualTo("Existing negative"));
        Assert.That(result.NeutralResponse, Is.EqualTo("Existing neutral"));
        Assert.That(result.AllowAutoResponse, Is.True);
    }

    [Test]
    public void UpdateAsync_ShouldThrow_WhenBusinessNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var request = new UpdateBusinessAutoResponseRequest { AllowAutoResponse = true };

        _businessRepositoryMock
            .Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync((Business?)null);

        // Act & Assert
        Assert.ThrowsAsync<BusinessNotFoundException>(
            async () => await _service.UpdateAsync(businessId, request));
    }

    [Test]
    public void UpdateAsync_ShouldThrow_WhenAutoResponseNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var request = new UpdateBusinessAutoResponseRequest { AllowAutoResponse = true };

        _businessRepositoryMock
            .Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(business);

        _autoResponseRepositoryMock
            .Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync((BusinessAutoResponse?)null);

        // Act & Assert
        Assert.ThrowsAsync<BusinessNotFoundException>(
            async () => await _service.UpdateAsync(businessId, request));
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdatePositiveResponseOnly()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var existingAutoResponse = new BusinessAutoResponse
        {
            BusinessId = businessId,
            PositiveResponse = "Old positive",
            NegativeResponse = "Existing negative",
            NeutralResponse = "Existing neutral",
            AllowAutoResponse = true
        };

        var request = new UpdateBusinessAutoResponseRequest
        {
            PositiveResponse = "Updated positive"
        };

        _businessRepositoryMock
            .Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync(business);

        _autoResponseRepositoryMock
            .Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync(existingAutoResponse);

        // Act
        var result = await _service.UpdateAsync(businessId, request);

        // Assert
        Assert.That(result.PositiveResponse, Is.EqualTo("Updated positive"));
        Assert.That(result.NegativeResponse, Is.EqualTo("Existing negative"));
        Assert.That(result.NeutralResponse, Is.EqualTo("Existing neutral"));
        Assert.That(result.AllowAutoResponse, Is.True);
    }
}
