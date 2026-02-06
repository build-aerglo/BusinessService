using BusinessService.Application.DTOs.Verification;
using BusinessService.Application.Services;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace BusinessService.Application.Tests.Services;

[TestFixture]
public class BusinessVerificationServiceTests
{
    private Mock<IBusinessVerificationRepository> _verificationRepoMock = null!;
    private Mock<IBusinessRepository> _businessRepoMock = null!;
    private Mock<IIdVerificationRequestRepository> _idVerificationRequestRepoMock = null!;
    private BusinessVerificationService _service = null!;

    [SetUp]
    public void Setup()
    {
        _verificationRepoMock = new Mock<IBusinessVerificationRepository>();
        _businessRepoMock = new Mock<IBusinessRepository>();
        _idVerificationRequestRepoMock = new Mock<IIdVerificationRequestRepository>();

        _service = new BusinessVerificationService(
            _verificationRepoMock.Object,
            _businessRepoMock.Object,
            _idVerificationRequestRepoMock.Object
        );
    }

    // ---------------------------------------------------------
    // SUBMIT ID VERIFICATION TESTS
    // ---------------------------------------------------------
    [Test]
    public async Task SubmitIdVerificationAsync_ShouldInsertRequest_WhenBusinessExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var request = new SubmitIdVerificationRequest
        {
            BusinessId = businessId,
            IdVerificationType = "CAC",
            IdVerificationNumber = "RC123456",
            IdVerificationUrl = "https://example.com/cac.pdf",
            IdVerificationName = "Test Business CAC"
        };

        var business = new Business { Id = businessId, Name = "Test Business" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);

        // Act
        await _service.SubmitIdVerificationAsync(request);

        // Assert - should insert into id_verification_request
        _idVerificationRequestRepoMock.Verify(r => r.AddAsync(It.Is<IdVerificationRequest>(
            req => req.BusinessId == businessId &&
                   req.IdVerificationType == "CAC" &&
                   req.IdVerificationNumber == "RC123456" &&
                   req.IdVerificationUrl == "https://example.com/cac.pdf" &&
                   req.IdVerificationName == "Test Business CAC"
        )), Times.Once);

        // Assert - should update business_verification status
        _verificationRepoMock.Verify(r => r.UpdateIdVerificationStatusAsync(
            businessId,
            false,      // id_verified should be set to false
            "pending"   // id_verification_status should be 'pending'
        ), Times.Once);
    }

    [Test]
    public async Task SubmitIdVerificationAsync_ShouldWorkWithNullableFields()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var request = new SubmitIdVerificationRequest
        {
            BusinessId = businessId,
            IdVerificationType = "TIN",
            IdVerificationNumber = null,
            IdVerificationUrl = null,
            IdVerificationName = null
        };

        var business = new Business { Id = businessId, Name = "Test Business" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);

        // Act
        await _service.SubmitIdVerificationAsync(request);

        // Assert - should insert into id_verification_request with nullable fields
        _idVerificationRequestRepoMock.Verify(r => r.AddAsync(It.Is<IdVerificationRequest>(
            req => req.BusinessId == businessId &&
                   req.IdVerificationType == "TIN" &&
                   req.IdVerificationNumber == null &&
                   req.IdVerificationUrl == null &&
                   req.IdVerificationName == null
        )), Times.Once);
    }

    [Test]
    public void SubmitIdVerificationAsync_ShouldThrow_WhenBusinessNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var request = new SubmitIdVerificationRequest
        {
            BusinessId = businessId,
            IdVerificationType = "CAC",
            IdVerificationNumber = "RC123456",
            IdVerificationUrl = "https://example.com/cac.pdf"
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync((Business?)null);

        // Act
        Func<Task> act = async () => await _service.SubmitIdVerificationAsync(request);

        // Assert
        act.Should().ThrowAsync<BusinessNotFoundException>()
            .WithMessage($"Business with ID {businessId} not found");
    }

    // ---------------------------------------------------------
    // GET VERIFICATION STATUS TESTS
    // ---------------------------------------------------------
    [Test]
    public async Task GetVerificationStatusAsync_ShouldReturnDto_WhenVerificationExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CacVerified = true,
            PhoneVerified = true,
            EmailVerified = false,
            AddressVerified = false,
            IdVerified = false,
            IdVerificationStatus = "pending",
            OnlinePresenceVerified = false,
            OtherIdsVerified = false,
            BusinessDomainEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _verificationRepoMock.Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync(verification);

        // Act
        var result = await _service.GetVerificationStatusAsync(businessId);

        // Assert
        result.Should().NotBeNull();
        result.BusinessId.Should().Be(businessId);
        result.CacVerified.Should().BeTrue();
        result.PhoneVerified.Should().BeTrue();
        result.EmailVerified.Should().BeFalse();
        result.IdVerified.Should().BeFalse();
        result.IdVerificationStatus.Should().Be("pending");
    }

    [Test]
    public void GetVerificationStatusAsync_ShouldThrow_WhenVerificationNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();

        _verificationRepoMock.Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync((BusinessVerification?)null);

        // Act
        Func<Task> act = async () => await _service.GetVerificationStatusAsync(businessId);

        // Assert
        act.Should().ThrowAsync<VerificationNotFoundException>();
    }

    // ---------------------------------------------------------
    // CREATE VERIFICATION TESTS
    // ---------------------------------------------------------
    [Test]
    public async Task CreateVerificationAsync_ShouldCreateEntry_WhenBusinessExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _verificationRepoMock.Setup(r => r.ExistsByBusinessIdAsync(businessId)).ReturnsAsync(false);

        // Act
        var result = await _service.CreateVerificationAsync(businessId);

        // Assert
        result.Should().NotBeNull();
        result.BusinessId.Should().Be(businessId);
        _verificationRepoMock.Verify(r => r.AddAsync(It.Is<BusinessVerification>(
            v => v.BusinessId == businessId
        )), Times.Once);
    }

    [Test]
    public void CreateVerificationAsync_ShouldThrow_WhenBusinessNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId))
            .ReturnsAsync((Business?)null);

        // Act
        Func<Task> act = async () => await _service.CreateVerificationAsync(businessId);

        // Assert
        act.Should().ThrowAsync<BusinessNotFoundException>();
    }

    [Test]
    public void CreateVerificationAsync_ShouldThrow_WhenVerificationAlreadyExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _verificationRepoMock.Setup(r => r.ExistsByBusinessIdAsync(businessId)).ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _service.CreateVerificationAsync(businessId);

        // Assert
        act.Should().ThrowAsync<VerificationRequiredException>();
    }

    // ---------------------------------------------------------
    // VERIFY REQUIREMENT TESTS
    // ---------------------------------------------------------
    [Test]
    public async Task VerifyRequirementAsync_ShouldUpdateCacVerification_WhenCacType()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var request = new VerifyRequirementRequest
        {
            BusinessId = businessId,
            RequirementType = VerificationRequirementType.Cac,
            Name = "Test Business",
            Value = "RC123456"
        };

        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CacVerified = false
        };

        _verificationRepoMock.Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync(verification);

        // Act
        var result = await _service.VerifyRequirementAsync(request);

        // Assert
        result.CacVerified.Should().BeTrue();
        _verificationRepoMock.Verify(r => r.UpdateAsync(It.Is<BusinessVerification>(
            v => v.CacVerified == true && v.CacNumber == "RC123456"
        )), Times.Once);
    }

    [Test]
    public async Task VerifyRequirementAsync_ShouldCreateVerification_WhenNotExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var request = new VerifyRequirementRequest
        {
            BusinessId = businessId,
            RequirementType = VerificationRequirementType.Phone,
            Value = "+1234567890"
        };

        _verificationRepoMock.Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync((BusinessVerification?)null);

        // Act
        var result = await _service.VerifyRequirementAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.PhoneVerified.Should().BeTrue();
        _verificationRepoMock.Verify(r => r.AddAsync(It.Is<BusinessVerification>(
            v => v.BusinessId == businessId
        )), Times.Once);
    }

    // ---------------------------------------------------------
    // REVERIFICATION TESTS
    // ---------------------------------------------------------
    [Test]
    public async Task TriggerReverificationAsync_ShouldSetReverificationFlag()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var reason = "Information changed";
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            RequiresReverification = false
        };

        _verificationRepoMock.Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync(verification);

        // Act
        await _service.TriggerReverificationAsync(businessId, reason);

        // Assert
        _verificationRepoMock.Verify(r => r.UpdateAsync(It.Is<BusinessVerification>(
            v => v.RequiresReverification == true && v.ReverificationReason == reason
        )), Times.Once);
    }

    [Test]
    public async Task CompleteReverificationAsync_ShouldClearReverificationFlag()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            RequiresReverification = true,
            ReverificationReason = "Some reason"
        };

        _verificationRepoMock.Setup(r => r.FindByBusinessIdAsync(businessId))
            .ReturnsAsync(verification);

        // Act
        await _service.CompleteReverificationAsync(businessId);

        // Assert
        _verificationRepoMock.Verify(r => r.UpdateAsync(It.Is<BusinessVerification>(
            v => v.RequiresReverification == false && v.ReverificationReason == null
        )), Times.Once);
    }
}
