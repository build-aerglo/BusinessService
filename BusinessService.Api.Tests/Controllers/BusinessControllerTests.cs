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
            new List<FaqDto>(),
            "BASE64_TEST_QR",   // NEW ARGUMENT ✔️
            "approved",
            null,
            null,
            null,
            null,
            4.92m
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
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };

        _serviceMock.Setup(s => s.UpdateBusinessAsync(id, request))
            .ThrowsAsync(new BusinessNotFoundException("Business not found."));

        var result = await _controller.UpdateBusiness(id, request);

        var notFound = result as ObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
        notFound.Value.Should().BeEquivalentTo(new { error = "Business not found." });
    }

    [Test]
    public async Task UpdateBusiness_ShouldReturnConflict_WhenBusinessConflict()
    {
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };

        _serviceMock.Setup(s => s.UpdateBusinessAsync(id, request))
            .ThrowsAsync(new BusinessConflictException("Business conflict."));

        var result = await _controller.UpdateBusiness(id, request);

        var conflict = result as ObjectResult;
        conflict.Should().NotBeNull();
        conflict!.StatusCode.Should().Be(409);
        conflict.Value.Should().BeEquivalentTo(new { error = "Business conflict." });
    }
    
    [Test]
    public async Task GetBusinessesByCategory_ShouldReturnOk_WhenBusinessesExist()
    {
        var categoryId = Guid.NewGuid();

        var expected = new List<BusinessSummaryResponseDto>
        {
            new BusinessSummaryResponseDto(Guid.NewGuid(), "Acme Repairs", 4.8m, 210, false, null, null ,null, null, null, null, true, null, null, null),
            new BusinessSummaryResponseDto(Guid.NewGuid(), "FixIt Hub", 4.2m, 90, false, null, null ,null, null, null, null, true, null, null, null)
        };

        _serviceMock
            .Setup(s => s.GetBusinessesByCategoryAsync(categoryId))
            .ReturnsAsync(expected);

        var result = await _controller.GetBusinessesByCategory(categoryId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);

        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task GetBusinessesByCategory_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        var categoryId = Guid.NewGuid();

        _serviceMock
            .Setup(s => s.GetBusinessesByCategoryAsync(categoryId))
            .ThrowsAsync(new CategoryNotFoundException("Category not found."));

        var result = await _controller.GetBusinessesByCategory(categoryId);

        var notFound = result as ObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);

        notFound.Value.Should().BeEquivalentTo(new { error = "Category not found." });
    }
    [Test]
    public async Task GetBusinessesByCategory_ShouldReturnOk_WithEmptyList_WhenNoBusinessesMatch()
    {
        var categoryId = Guid.NewGuid();

        var expected = new List<BusinessSummaryResponseDto>();

        _serviceMock
            .Setup(s => s.GetBusinessesByCategoryAsync(categoryId))
            .ReturnsAsync(expected);

        var result = await _controller.GetBusinessesByCategory(categoryId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);

        var response = ok.Value as List<BusinessSummaryResponseDto>;
        response.Should().NotBeNull();
        response!.Count.Should().Be(0);
    }
    
    // business claim tests
    [Test]
    public async Task ClaimBusiness_ShouldReturnOk_WhenClaimIsSuccessful()
    {
        // Arrange
        var dto = new BusinessClaimsDto
        (
            Guid.NewGuid(),
            "John Doe",
            "Owner",
            "john@example.com", 
            "+1234567890"
        );

        _serviceMock.Setup(s => s.ClaimBusinessAsync(dto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ClaimBusiness(dto);

        // Assert
        result.Should().BeOfType<OkResult>();
        _serviceMock.Verify(s => s.ClaimBusinessAsync(dto), Times.Once);
    }

    [Test]
    public async Task ClaimBusiness_ShouldReturnNotFound_WhenBusinessDoesNotExist()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var dto = new BusinessClaimsDto
        (
            businessId,
            "John Doe",
            "Owner",
            "john@example.com",
            "+1234567890"
        );

        var exception = new BusinessNotFoundException($"Business {businessId} not found.");

        _serviceMock.Setup(s => s.ClaimBusinessAsync(dto))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.ClaimBusiness(dto);

        // Assert
        var notFound = result as ObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
        notFound.Value.Should().BeEquivalentTo(new { error = $"Business {businessId} not found." });
        
    }

    [Test]
    public async Task ClaimBusiness_ShouldReturnConflict_WhenBusinessIsAlreadyApproved()
    {
        // Arrange
        var dto = new BusinessClaimsDto
        (
            Guid.NewGuid(),
            "John Doe",
            "Owner",
            "john@example.com",
            "+1234567890"
        );

        var exception = new BusinessConflictException("Business already approved.");

        _serviceMock.Setup(s => s.ClaimBusinessAsync(dto))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.ClaimBusiness(dto);

        // Assert
        var conflict = result as ObjectResult;
        conflict.Should().NotBeNull();
        conflict!.StatusCode.Should().Be(409);
        conflict.Value.Should().BeEquivalentTo(new { error = "Business already approved." });

    }

    [Test]
    public async Task ClaimBusiness_ShouldReturnConflict_WhenBusinessApprovalIsInProgress()
    {
        // Arrange
        var dto = new BusinessClaimsDto
        (
            Guid.NewGuid(),
            "John Doe",
            "Owner",
            "john@example.com",
            "+1234567890"
        );

        var exception = new BusinessConflictException("Business approval in progress.");

        _serviceMock.Setup(s => s.ClaimBusinessAsync(dto))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.ClaimBusiness(dto);

        // Assert
        var conflict = result as ObjectResult;
        conflict.Should().NotBeNull();
        conflict!.StatusCode.Should().Be(409);
        conflict.Value.Should().BeEquivalentTo(new { error = "Business approval in progress." });
    
    }

    [Test]
    public async Task ClaimBusiness_ShouldCallServiceWithCorrectDto()
    {
        // Arrange
        var dto = new BusinessClaimsDto
        (
            Guid.NewGuid(),
            "John Doe",
            "Owner",
            "john@example.com",
            "+1234567890"
        );

        _serviceMock.Setup(s => s.ClaimBusinessAsync(It.IsAny<BusinessClaimsDto>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.ClaimBusiness(dto);

        // Assert
        _serviceMock.Verify(s => s.ClaimBusinessAsync(
            It.Is<BusinessClaimsDto>(d =>
                d.Id == dto.Id &&
                d.Role == dto.Role &&
                d.Name == dto.Name &&
                d.Email == dto.Email &&
                d.Phone == dto.Phone
            )), Times.Once);
    }
}
