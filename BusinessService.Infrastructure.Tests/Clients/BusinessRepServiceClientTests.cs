using System.Net;
using BusinessService.Application.Interfaces;
using BusinessService.Infrastructure.Clients;
using BusinessService.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace BusinessService.Infrastructure.Tests.Clients;

[TestFixture]
public class BusinessRepServiceClientTests
{
    private Mock<HttpMessageHandler> _mockHandler = null!;
    private Mock<ILogger<BusinessRepServiceClient>> _mockLogger = null!;
    private HttpClient _httpClient = null!;
    private BusinessRepServiceClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _mockLogger = new Mock<ILogger<BusinessRepServiceClient>>();

        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://fake-user-service.com")
        };

        _client = new BusinessRepServiceClient(_httpClient, _mockLogger.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        _httpClient.Dispose();
    }

    // ========== GetBusinessRepByIdAsync Tests ==========

    [Test]
    public async Task GetBusinessRepByIdAsync_ShouldReturnDto_WhenSuccessful()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var expectedDto = new BusinessRepDto(
            Id: businessRepId,
            BusinessId: businessId,
            UserId: userId,
            BranchName: "Main Branch",
            BranchAddress: "123 Main St",
            CreatedAt: DateTime.UtcNow
        );

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/business-rep/{businessRepId}")
            .ReturnsJsonResponse(expectedDto, HttpStatusCode.OK);

        // Act
        var result = await _client.GetBusinessRepByIdAsync(businessRepId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(businessRepId));
        Assert.That(result.BusinessId, Is.EqualTo(businessId));
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.BranchName, Is.EqualTo("Main Branch"));
    }

    [Test]
    public async Task GetBusinessRepByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/business-rep/{businessRepId}")
            .ReturnsResponse(HttpStatusCode.NotFound);

        // Act
        var result = await _client.GetBusinessRepByIdAsync(businessRepId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetBusinessRepByIdAsync_ShouldReturnNull_OnHttpRequestException()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/business-rep/{businessRepId}")
            .Throws<HttpRequestException>();

        // Act
        var result = await _client.GetBusinessRepByIdAsync(businessRepId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetBusinessRepByIdAsync_ShouldReturnNull_OnUnexpectedException()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/business-rep/{businessRepId}")
            .Throws<Exception>();

        // Act
        var result = await _client.GetBusinessRepByIdAsync(businessRepId);

        // Assert
        Assert.That(result, Is.Null);
    }

    // ========== GetParentRepByBusinessIdAsync Tests ==========

    [Test]
    public async Task GetParentRepByBusinessIdAsync_ShouldReturnDto_WhenSuccessful()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var businessRepId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var expectedDto = new BusinessRepDto(
            Id: businessRepId,
            BusinessId: businessId,
            UserId: userId,
            BranchName: "Parent Branch",
            BranchAddress: "456 Parent Ave",
            CreatedAt: DateTime.UtcNow.AddDays(-30) // Parent rep created earlier
        );

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/business-rep/parent/{businessId}")
            .ReturnsJsonResponse(expectedDto, HttpStatusCode.OK);

        // Act
        var result = await _client.GetParentRepByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(businessRepId));
        Assert.That(result.BusinessId, Is.EqualTo(businessId));
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.BranchName, Is.EqualTo("Parent Branch"));
    }

    [Test]
    public async Task GetParentRepByBusinessIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/business-rep/parent/{businessId}")
            .ReturnsResponse(HttpStatusCode.NotFound);

        // Act
        var result = await _client.GetParentRepByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetParentRepByBusinessIdAsync_ShouldReturnNull_OnHttpRequestException()
    {
        // Arrange
        var businessId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/business-rep/parent/{businessId}")
            .Throws<HttpRequestException>();

        // Act
        var result = await _client.GetParentRepByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetParentRepByBusinessIdAsync_ShouldReturnNull_OnUnexpectedException()
    {
        // Arrange
        var businessId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/business-rep/parent/{businessId}")
            .Throws<Exception>();

        // Act
        var result = await _client.GetParentRepByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetParentRepByBusinessIdAsync_ShouldReturnNull_OnInternalServerError()
    {
        // Arrange
        var businessId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/business-rep/parent/{businessId}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server error");

        // Act
        var result = await _client.GetParentRepByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Null);
    }
}