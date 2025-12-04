using BusinessService.Application.DTOs;
using BusinessService.Application.Interfaces;
using BusinessService.Application.Services;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using FluentAssertions;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace BusinessService.Application.Tests.Services;

[TestFixture]
public class BusinessServiceTests
{
    private Mock<IBusinessRepository> _businessRepoMock = null!;
    private Mock<ICategoryRepository> _categoryRepoMock = null!;
    private Mock<IQrCodeService> _qrCodeServiceMock = null!;
    private Mock<IBusinessSearchProducer> _searchProducerMock = null!;
    private Application.Services.BusinessService _service = null!;

    [SetUp]
    public void Setup()
    {
        _businessRepoMock = new Mock<IBusinessRepository>();
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _qrCodeServiceMock = new Mock<IQrCodeService>();
        _searchProducerMock = new Mock<IBusinessSearchProducer>();

        // Always return a fixed Base64 encoded QR code for predictable tests
        _qrCodeServiceMock.Setup(q => q.GenerateQrCodeBase64(It.IsAny<string>()))
            .Returns("BASE64_QR_CODE_TEST_VALUE");

        _service = new Application.Services.BusinessService(
            _businessRepoMock.Object,
            _categoryRepoMock.Object,
            _qrCodeServiceMock.Object,
            _searchProducerMock.Object
        );
    }

    // ---------------------------------------------------------
    // CREATE BUSINESS TESTS
    // ---------------------------------------------------------
    [Test]
    public async Task CreateBusinessAsync_ShouldCreateBusiness_WhenValid()
    {
        var request = new CreateBusinessRequest
        {
            Name = "Alpha Coffee",
            Website = "https://alphacoffee.com",
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        };

        var category = new Category { Id = request.CategoryIds[0], Name = "Coffee" };

        _businessRepoMock.Setup(r => r.ExistsByNameAsync(request.Name)).ReturnsAsync(false);
        _categoryRepoMock.Setup(r => r.FindAllByIdsAsync(request.CategoryIds))
            .ReturnsAsync(new List<Category> { category });

        var result = await _service.CreateBusinessAsync(request);

        // Assertions
        result.Should().NotBeNull();
        result.Name.Should().Be("Alpha Coffee");
        result.Categories.Should().ContainSingle(c => c.Name == "Coffee");
        result.QrCodeBase64.Should().Be("BASE64_QR_CODE_TEST_VALUE");

        _businessRepoMock.Verify(r => r.AddAsync(It.IsAny<Business>()), Times.Once);
        _qrCodeServiceMock.Verify(q => q.GenerateQrCodeBase64(It.IsAny<string>()), Times.Once);

        // NEW: Ensure Search Service is notified
        _searchProducerMock.Verify(s => s.PublishBusinessCreatedAsync(It.IsAny<BusinessDto>()), Times.Once);
    }

    [Test]
    public void CreateBusinessAsync_ShouldThrow_WhenNameAlreadyExists()
    {
        var request = new CreateBusinessRequest
        {
            Name = "Duplicate Shop",
            Website = "https://dup.com",
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        };

        _businessRepoMock.Setup(r => r.ExistsByNameAsync(request.Name)).ReturnsAsync(true);

        Func<Task> act = async () => await _service.CreateBusinessAsync(request);

        act.Should().ThrowAsync<BusinessAlreadyExistsException>()
            .WithMessage("Business name 'Duplicate Shop' already exists.");
    }

    [Test]
    public void CreateBusinessAsync_ShouldThrow_WhenCategoryNotFound()
    {
        var request = new CreateBusinessRequest
        {
            Name = "Shop X",
            Website = "https://shopx.com",
            CategoryIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        _businessRepoMock.Setup(r => r.ExistsByNameAsync(request.Name)).ReturnsAsync(false);

        // return only 1 category even though request has 2 → triggers error
        _categoryRepoMock.Setup(r => r.FindAllByIdsAsync(request.CategoryIds))
            .ReturnsAsync(new List<Category> { new Category { Id = request.CategoryIds[0] } });

        Func<Task> act = async () => await _service.CreateBusinessAsync(request);

        act.Should().ThrowAsync<CategoryNotFoundException>()
            .WithMessage("One or more categories not found.");
    }

    // ---------------------------------------------------------
    // UPDATE BUSINESS TESTS
    // ---------------------------------------------------------
    [Test]
    public async Task UpdateBusinessAsync_ShouldUpdateBusiness_WhenValid()
    {
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };
        var business = new Business { Id = id, Name = "Old Name" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(business);

        var result = await _service.UpdateBusinessAsync(id, request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");

        _businessRepoMock.Verify(r => r.UpdateProfileAsync(It.Is<Business>(b => b.Name == "Updated Name")), Times.Once);

        // QR code should NOT regenerate during update
        _qrCodeServiceMock.Verify(q => q.GenerateQrCodeBase64(It.IsAny<string>()), Times.Never);

        // NEW: Ensure Search Service update is called
        _searchProducerMock.Verify(s => s.PublishBusinessUpdatedAsync(It.IsAny<BusinessDto>()), Times.Once);
    }

    [Test]
    public void UpdateBusinessAsync_ShouldThrow_WhenBusinessNotFound()
    {
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((Business?)null);

        Func<Task> act = async () => await _service.UpdateBusinessAsync(id, request);

        act.Should().ThrowAsync<BusinessNotFoundException>()
            .WithMessage($"Business {id} not found.");
    }

    [Test]
    public void UpdateBusinessAsync_ShouldThrow_WhenUniqueConstraintViolated()
    {
        var id = Guid.NewGuid();
        var request = new UpdateBusinessRequest { Name = "Updated Name" };
        var business = new Business { Id = id, Name = "Old Name" };

        _businessRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(business);

        _businessRepoMock.Setup(r => r.UpdateProfileAsync(It.IsAny<Business>()))
            .ThrowsAsync(new PostgresException("msg", "severity", "23505", "detail"));

        Func<Task> act = async () => await _service.UpdateBusinessAsync(id, request);

        act.Should().ThrowAsync<BusinessConflictException>()
            .WithMessage("The provided business email or access username is already in use.");
    }
}
