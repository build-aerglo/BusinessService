using BusinessService.Application.DTOs.Claims;
using BusinessService.Application.Services;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace BusinessService.Application.Tests.Services;

[TestFixture]
public class BusinessClaimServiceTests
{
    private Mock<IBusinessClaimRequestRepository> _claimRepoMock = null!;
    private Mock<IBusinessRepository> _businessRepoMock = null!;
    private BusinessClaimService _service = null!;

    [SetUp]
    public void Setup()
    {
        _claimRepoMock = new Mock<IBusinessClaimRequestRepository>();
        _businessRepoMock = new Mock<IBusinessRepository>();
        _service = new BusinessClaimService(_claimRepoMock.Object, _businessRepoMock.Object);
    }

    [Test]
    public async Task SubmitClaimAsync_ShouldSetBusinessCategory_WhenCategoryIdProvided()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var business = new Business { Id = businessId, Name = "Test Business", IsVerified = false };

        var request = new SubmitBusinessClaimRequest
        {
            BusinessId = businessId,
            FullName = "John Doe",
            Email = "john@example.com",
            PhoneNumber = "+1234567890",
            Role = "Owner",
            CategoryId = categoryId
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _claimRepoMock.Setup(r => r.ExistsPendingByBusinessIdAsync(businessId))
            .ReturnsAsync(false);
        _claimRepoMock.Setup(r => r.AddAsync(It.IsAny<BusinessClaimRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitClaimAsync(request, userId);

        // Assert
        _claimRepoMock.Verify(r => r.AddAsync(It.Is<BusinessClaimRequest>(c =>
            c.BusinessCategory == categoryId &&
            c.BusinessId == businessId &&
            c.FullName == "John Doe"
        )), Times.Once);
    }

    [Test]
    public async Task SubmitClaimAsync_ShouldSetBusinessCategoryNull_WhenCategoryIdNotProvided()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var business = new Business { Id = businessId, Name = "Test Business", IsVerified = false };

        var request = new SubmitBusinessClaimRequest
        {
            BusinessId = businessId,
            FullName = "Jane Doe",
            Email = "jane@example.com",
            PhoneNumber = "+1234567890",
            Role = "Manager"
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _claimRepoMock.Setup(r => r.ExistsPendingByBusinessIdAsync(businessId))
            .ReturnsAsync(false);
        _claimRepoMock.Setup(r => r.AddAsync(It.IsAny<BusinessClaimRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitClaimAsync(request, userId);

        // Assert
        _claimRepoMock.Verify(r => r.AddAsync(It.Is<BusinessClaimRequest>(c =>
            c.BusinessCategory == null &&
            c.BusinessId == businessId
        )), Times.Once);
    }
}
