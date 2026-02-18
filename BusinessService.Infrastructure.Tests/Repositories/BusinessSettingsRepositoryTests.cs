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
public class BusinessSettingsRepositoryTests
{
    private DapperContext _context = null!;
    private BusinessSettingsRepository _repository = null!;
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
        _repository = new BusinessSettingsRepository(_context);

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
        conn.Execute("DELETE FROM business_settings WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'RepoTest%');");
        conn.Execute("DELETE FROM business_rep_settings WHERE business_rep_id IN (SELECT id FROM business_reps WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'RepoTest%'));");
        conn.Execute("DELETE FROM business_auto_response WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'RepoTest%');");
        conn.Execute("DELETE FROM business_reps WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'RepoTest%');");
        conn.Execute("DELETE FROM business WHERE name LIKE 'RepoTest%';");
        conn.Execute("DELETE FROM users WHERE username LIKE 'repotest%';");
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

    private async Task<Guid> CreateTestUser(string username)
    {
        var userId = Guid.NewGuid();
        const string sql = @"
            INSERT INTO users (id, username, email, user_type, join_date, created_at, updated_at)
            VALUES (@Id, @Username, @Email, 'business_user', now(), now(), now());
        ";

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new { 
            Id = userId, 
            Username = username,
            Email = $"{username}@test.com"
        });
        return userId;
    }

    private async Task<Guid> CreateTestBusinessRep(Guid businessId, Guid userId)
    {
        var repId = Guid.NewGuid();
        const string sql = @"
            INSERT INTO business_reps (id, business_id, user_id, branch_name, branch_address, created_at, updated_at)
            VALUES (@Id, @BusinessId, @UserId, 'Test Branch', 'Test Address', now(), now());
        ";

        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = repId, BusinessId = businessId, UserId = userId });
        return repId;
    }

    // ========== Business Settings Tests ==========

    [Test]
    public async Task AddBusinessSettingsAsync_ShouldInsertSettings()
    {
        // Arrange
        var businessId = await CreateTestBusiness("RepoTest Business 1");
        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ReviewsPrivate = true,
            DndModeEnabled = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddBusinessSettingsAsync(settings);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(settings.Id));

        var retrieved = await _repository.FindBusinessSettingsByBusinessIdAsync(businessId);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ReviewsPrivate, Is.True);
    }

    [Test]
    public async Task FindBusinessSettingsByBusinessIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentBusinessId = Guid.NewGuid();

        // Act
        var result = await _repository.FindBusinessSettingsByBusinessIdAsync(nonExistentBusinessId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateBusinessSettingsAsync_ShouldModifySettings()
    {
        // Arrange
        var businessId = await CreateTestBusiness("RepoTest Business 2");
        var settings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ReviewsPrivate = false,
            DndModeEnabled = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddBusinessSettingsAsync(settings);

        // Act
        settings.ReviewsPrivate = true;
        settings.EnableDndMode(48);
        await _repository.UpdateBusinessSettingsAsync(settings);

        // Assert
        var updated = await _repository.FindBusinessSettingsByBusinessIdAsync(businessId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.ReviewsPrivate, Is.True);
        Assert.That(updated.DndModeEnabled, Is.True);
        Assert.That(updated.DndModeExpiresAt, Is.Not.Null);
    }

    // ========== Rep Settings Tests ==========

    [Test]
    public async Task AddRepSettingsAsync_ShouldInsertSettings()
    {
        // Arrange
        var businessId = await CreateTestBusiness("RepoTest Business 3");
        var userId = await CreateTestUser("repotest3");
        var repId = await CreateTestBusinessRep(businessId, userId);

        var settings = new BusinessRepSettings
        {
            Id = Guid.NewGuid(),
            BusinessRepId = repId,
            DarkMode = true,
            NotificationPreferences = "{\"email\":true,\"whatsapp\":false,\"inApp\":true}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddRepSettingsAsync(settings);

        // Assert
        Assert.That(result, Is.Not.Null);

        var retrieved = await _repository.FindRepSettingsByRepIdAsync(repId);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.DarkMode, Is.True);
    }

    [Test]
    public async Task UpdateRepSettingsAsync_ShouldModifySettings()
    {
        // Arrange
        var businessId = await CreateTestBusiness("RepoTest Business 4");
        var userId = await CreateTestUser("repotest4");
        var repId = await CreateTestBusinessRep(businessId, userId);

        var settings = new BusinessRepSettings
        {
            Id = Guid.NewGuid(),
            BusinessRepId = repId,
            DarkMode = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddRepSettingsAsync(settings);

        // Act
        settings.DarkMode = true;
        settings.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateRepSettingsAsync(settings);

        // Assert
        var updated = await _repository.FindRepSettingsByRepIdAsync(repId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.DarkMode, Is.True);
    }

    [Test]
    public async Task GetExpiredDndModeSettingsAsync_ShouldReturnExpiredOnly()
    {
        // Arrange
        var business1 = await CreateTestBusiness("RepoTest Business 5");
        var business2 = await CreateTestBusiness("RepoTest Business 6");

        var expiredSettings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = business1,
            DndModeEnabled = true,
            DndModeEnabledAt = DateTime.UtcNow.AddHours(-72),
            DndModeExpiresAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var activeSettings = new BusinessSettings
        {
            Id = Guid.NewGuid(),
            BusinessId = business2,
            DndModeEnabled = true,
            DndModeEnabledAt = DateTime.UtcNow,
            DndModeExpiresAt = DateTime.UtcNow.AddHours(48),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddBusinessSettingsAsync(expiredSettings);
        await _repository.AddBusinessSettingsAsync(activeSettings);

        // Act
        var result = await _repository.GetExpiredDndModeSettingsAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First().BusinessId, Is.EqualTo(business1));
    }
}