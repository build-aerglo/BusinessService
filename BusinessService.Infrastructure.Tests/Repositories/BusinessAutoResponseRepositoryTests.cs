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
public class BusinessAutoResponseRepositoryTests
{
    private DapperContext _context = null!;
    private BusinessAutoResponseRepository _repository = null!;
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
        _repository = new BusinessAutoResponseRepository(_context);

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
        conn.Execute("DELETE FROM business_auto_response WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'AutoRespRepoTest%');");
        conn.Execute("DELETE FROM business WHERE name LIKE 'AutoRespRepoTest%';");
    }

    private async Task<Guid> CreateTestBusinessWithAutoResponse(string name)
    {
        var businessId = Guid.NewGuid();
        const string businessSql = @"
            INSERT INTO business (id, name, is_branch, avg_rating, review_count)
            VALUES (@Id, @Name, false, 0, 0);
        ";

        const string autoResponseSql = @"
            INSERT INTO business_auto_response (business_id, positive_response, negative_response, neutral_response, allow_auto_response)
            VALUES (@BusinessId, NULL, NULL, NULL, false);
        ";

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(businessSql, new { Id = businessId, Name = name });
        await conn.ExecuteAsync(autoResponseSql, new { BusinessId = businessId });
        return businessId;
    }

    // ========== FindByBusinessIdAsync Tests ==========

    [Test]
    public async Task FindByBusinessIdAsync_ShouldReturnAutoResponse_WhenExists()
    {
        // Arrange
        var businessId = await CreateTestBusinessWithAutoResponse("AutoRespRepoTest Business 1");

        // Act
        var result = await _repository.FindByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.BusinessId, Is.EqualTo(businessId));
        Assert.That(result.AllowAutoResponse, Is.False);
        Assert.That(result.PositiveResponse, Is.Null);
        Assert.That(result.NegativeResponse, Is.Null);
        Assert.That(result.NeutralResponse, Is.Null);
    }

    [Test]
    public async Task FindByBusinessIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.FindByBusinessIdAsync(nonExistentId);

        // Assert
        Assert.That(result, Is.Null);
    }

    // ========== UpdateAsync Tests ==========

    [Test]
    public async Task UpdateAsync_ShouldUpdateAllFields()
    {
        // Arrange
        var businessId = await CreateTestBusinessWithAutoResponse("AutoRespRepoTest Business 2");
        var autoResponse = new BusinessAutoResponse
        {
            BusinessId = businessId,
            PositiveResponse = "Thank you for your positive review!",
            NegativeResponse = "We're sorry to hear about your experience.",
            NeutralResponse = "Thank you for your feedback.",
            AllowAutoResponse = true
        };

        // Act
        await _repository.UpdateAsync(autoResponse);

        // Assert
        var updated = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.PositiveResponse, Is.EqualTo("Thank you for your positive review!"));
        Assert.That(updated.NegativeResponse, Is.EqualTo("We're sorry to hear about your experience."));
        Assert.That(updated.NeutralResponse, Is.EqualTo("Thank you for your feedback."));
        Assert.That(updated.AllowAutoResponse, Is.True);
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateOnlyAllowAutoResponse()
    {
        // Arrange
        var businessId = await CreateTestBusinessWithAutoResponse("AutoRespRepoTest Business 3");

        // First update with responses
        var initialUpdate = new BusinessAutoResponse
        {
            BusinessId = businessId,
            PositiveResponse = "Initial positive",
            NegativeResponse = "Initial negative",
            NeutralResponse = "Initial neutral",
            AllowAutoResponse = false
        };
        await _repository.UpdateAsync(initialUpdate);

        // Act - Update only AllowAutoResponse
        var autoResponse = new BusinessAutoResponse
        {
            BusinessId = businessId,
            PositiveResponse = "Initial positive",
            NegativeResponse = "Initial negative",
            NeutralResponse = "Initial neutral",
            AllowAutoResponse = true
        };
        await _repository.UpdateAsync(autoResponse);

        // Assert
        var updated = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.AllowAutoResponse, Is.True);
        Assert.That(updated.PositiveResponse, Is.EqualTo("Initial positive"));
    }

    [Test]
    public async Task UpdateAsync_ShouldSetResponsesToNull()
    {
        // Arrange
        var businessId = await CreateTestBusinessWithAutoResponse("AutoRespRepoTest Business 4");

        // First set some values
        var initialUpdate = new BusinessAutoResponse
        {
            BusinessId = businessId,
            PositiveResponse = "Some response",
            NegativeResponse = "Some response",
            NeutralResponse = "Some response",
            AllowAutoResponse = true
        };
        await _repository.UpdateAsync(initialUpdate);

        // Act - Set responses back to null
        var autoResponse = new BusinessAutoResponse
        {
            BusinessId = businessId,
            PositiveResponse = null,
            NegativeResponse = null,
            NeutralResponse = null,
            AllowAutoResponse = false
        };
        await _repository.UpdateAsync(autoResponse);

        // Assert
        var updated = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.PositiveResponse, Is.Null);
        Assert.That(updated.NegativeResponse, Is.Null);
        Assert.That(updated.NeutralResponse, Is.Null);
        Assert.That(updated.AllowAutoResponse, Is.False);
    }

    // ========== Integration Flow Test ==========

    [Test]
    public async Task FullFlow_CreateBusinessAndManageAutoResponse()
    {
        // Arrange - Create business with auto-response
        var businessId = await CreateTestBusinessWithAutoResponse("AutoRespRepoTest Business 5");

        // Step 1: Verify initial state
        var initial = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(initial, Is.Not.Null);
        Assert.That(initial!.AllowAutoResponse, Is.False);

        // Step 2: Enable auto-response with templates
        var enabledResponse = new BusinessAutoResponse
        {
            BusinessId = businessId,
            PositiveResponse = "Thanks for the great review!",
            NegativeResponse = "We apologize for any inconvenience.",
            NeutralResponse = "Thank you for sharing your experience.",
            AllowAutoResponse = true
        };
        await _repository.UpdateAsync(enabledResponse);

        var afterEnable = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(afterEnable!.AllowAutoResponse, Is.True);
        Assert.That(afterEnable.PositiveResponse, Is.Not.Null);

        // Step 3: Disable auto-response but keep templates
        var disabledResponse = new BusinessAutoResponse
        {
            BusinessId = businessId,
            PositiveResponse = "Thanks for the great review!",
            NegativeResponse = "We apologize for any inconvenience.",
            NeutralResponse = "Thank you for sharing your experience.",
            AllowAutoResponse = false
        };
        await _repository.UpdateAsync(disabledResponse);

        var afterDisable = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(afterDisable!.AllowAutoResponse, Is.False);
        Assert.That(afterDisable.PositiveResponse, Is.EqualTo("Thanks for the great review!"));
    }
}
