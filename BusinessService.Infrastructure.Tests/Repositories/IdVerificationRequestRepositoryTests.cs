using BusinessService.Domain.Entities;
using BusinessService.Infrastructure.Context;
using BusinessService.Infrastructure.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NUnit.Framework;

namespace BusinessService.Infrastructure.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class IdVerificationRequestRepositoryTests
{
    private DapperContext _context = null!;
    private IdVerificationRequestRepository _repository = null!;
    private string _connectionString = null!;

    [SetUp]
    public void Setup()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        _connectionString = configuration.GetConnectionString("PostgresConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        _context = new DapperContext(configuration);
        _repository = new IdVerificationRequestRepository(_context);

        CleanupTestData();
    }

    [TearDown]
    public void TearDown()
    {
        CleanupTestData();
    }

    private void CleanupTestData()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Execute("DELETE FROM id_verification_request WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'IdVerRepoTest%');");
        conn.Execute("DELETE FROM business_verification WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'IdVerRepoTest%');");
        conn.Execute("DELETE FROM business WHERE name LIKE 'IdVerRepoTest%';");
    }

    private async Task<Guid> CreateTestBusiness(string name)
    {
        var businessId = Guid.NewGuid();
        const string sql = @"
            INSERT INTO business (id, name, is_branch, avg_rating, review_count)
            VALUES (@Id, @Name, false, 0, 0);
        ";

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = businessId, Name = name });
        return businessId;
    }

    // ========== AddAsync Tests ==========

    [Test]
    public async Task AddAsync_ShouldInsertIdVerificationRequest()
    {
        // Arrange
        var businessId = await CreateTestBusiness("IdVerRepoTest Business 1");
        var request = new IdVerificationRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            IdVerificationType = "CAC",
            IdVerificationNumber = "RC123456",
            IdVerificationUrl = "https://example.com/cac.pdf",
            IdVerificationName = "Test Business CAC Document",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(request);

        // Assert
        var retrieved = await _repository.FindByIdAsync(request.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Id, Is.EqualTo(request.Id));
        Assert.That(retrieved.BusinessId, Is.EqualTo(businessId));
        Assert.That(retrieved.IdVerificationType, Is.EqualTo("CAC"));
        Assert.That(retrieved.IdVerificationNumber, Is.EqualTo("RC123456"));
        Assert.That(retrieved.IdVerificationUrl, Is.EqualTo("https://example.com/cac.pdf"));
        Assert.That(retrieved.IdVerificationName, Is.EqualTo("Test Business CAC Document"));
    }

    [Test]
    public async Task AddAsync_ShouldHandleNullableFields()
    {
        // Arrange
        var businessId = await CreateTestBusiness("IdVerRepoTest Business 2");
        var request = new IdVerificationRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            IdVerificationType = "TIN",
            IdVerificationNumber = null,
            IdVerificationUrl = null,
            IdVerificationName = null,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(request);

        // Assert
        var retrieved = await _repository.FindByIdAsync(request.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.IdVerificationType, Is.EqualTo("TIN"));
        Assert.That(retrieved.IdVerificationNumber, Is.Null);
        Assert.That(retrieved.IdVerificationUrl, Is.Null);
        Assert.That(retrieved.IdVerificationName, Is.Null);
    }

    // ========== FindByIdAsync Tests ==========

    [Test]
    public async Task FindByIdAsync_ShouldReturnRequest_WhenExists()
    {
        // Arrange
        var businessId = await CreateTestBusiness("IdVerRepoTest Business 3");
        var request = new IdVerificationRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            IdVerificationType = "LICENSE",
            IdVerificationNumber = "LIC-001",
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(request);

        // Act
        var result = await _repository.FindByIdAsync(request.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(request.Id));
    }

    [Test]
    public async Task FindByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.FindByIdAsync(nonExistentId);

        // Assert
        Assert.That(result, Is.Null);
    }

    // ========== FindByBusinessIdAsync Tests ==========

    [Test]
    public async Task FindByBusinessIdAsync_ShouldReturnMostRecentRequest()
    {
        // Arrange
        var businessId = await CreateTestBusiness("IdVerRepoTest Business 4");

        var olderRequest = new IdVerificationRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            IdVerificationType = "CAC",
            IdVerificationNumber = "RC-OLD",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var newerRequest = new IdVerificationRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            IdVerificationType = "TIN",
            IdVerificationNumber = "TIN-NEW",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(olderRequest);
        await _repository.AddAsync(newerRequest);

        // Act
        var result = await _repository.FindByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(newerRequest.Id));
        Assert.That(result.IdVerificationType, Is.EqualTo("TIN"));
        Assert.That(result.IdVerificationNumber, Is.EqualTo("TIN-NEW"));
    }

    [Test]
    public async Task FindByBusinessIdAsync_ShouldReturnNull_WhenNoRequests()
    {
        // Arrange
        var businessId = await CreateTestBusiness("IdVerRepoTest Business 5");

        // Act
        var result = await _repository.FindByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Null);
    }

    // ========== FindAllByBusinessIdAsync Tests ==========

    [Test]
    public async Task FindAllByBusinessIdAsync_ShouldReturnAllRequestsOrderedByCreatedAtDesc()
    {
        // Arrange
        var businessId = await CreateTestBusiness("IdVerRepoTest Business 6");

        var request1 = new IdVerificationRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            IdVerificationType = "CAC",
            IdVerificationNumber = "RC-001",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var request2 = new IdVerificationRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            IdVerificationType = "TIN",
            IdVerificationNumber = "TIN-001",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var request3 = new IdVerificationRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            IdVerificationType = "LICENSE",
            IdVerificationNumber = "LIC-001",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(request1);
        await _repository.AddAsync(request2);
        await _repository.AddAsync(request3);

        // Act
        var results = await _repository.FindAllByBusinessIdAsync(businessId);

        // Assert
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Id, Is.EqualTo(request3.Id)); // Most recent first
        Assert.That(results[1].Id, Is.EqualTo(request2.Id));
        Assert.That(results[2].Id, Is.EqualTo(request1.Id)); // Oldest last
    }

    [Test]
    public async Task FindAllByBusinessIdAsync_ShouldReturnEmptyList_WhenNoRequests()
    {
        // Arrange
        var businessId = await CreateTestBusiness("IdVerRepoTest Business 7");

        // Act
        var results = await _repository.FindAllByBusinessIdAsync(businessId);

        // Assert
        Assert.That(results, Is.Not.Null);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task FindAllByBusinessIdAsync_ShouldOnlyReturnRequestsForSpecificBusiness()
    {
        // Arrange
        var business1Id = await CreateTestBusiness("IdVerRepoTest Business 8a");
        var business2Id = await CreateTestBusiness("IdVerRepoTest Business 8b");

        var request1 = new IdVerificationRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = business1Id,
            IdVerificationType = "CAC",
            CreatedAt = DateTime.UtcNow
        };

        var request2 = new IdVerificationRequest
        {
            Id = Guid.NewGuid(),
            BusinessId = business2Id,
            IdVerificationType = "TIN",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(request1);
        await _repository.AddAsync(request2);

        // Act
        var results = await _repository.FindAllByBusinessIdAsync(business1Id);

        // Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].BusinessId, Is.EqualTo(business1Id));
        Assert.That(results[0].IdVerificationType, Is.EqualTo("CAC"));
    }
}
