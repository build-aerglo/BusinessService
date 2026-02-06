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
public class BusinessVerificationRepositoryTests
{
    private DapperContext _context = null!;
    private BusinessVerificationRepository _repository = null!;
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
        _repository = new BusinessVerificationRepository(_context);

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
        conn.Execute("DELETE FROM business_verification WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'BusVerRepoTest%');");
        conn.Execute("DELETE FROM business WHERE name LIKE 'BusVerRepoTest%';");
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
    public async Task AddAsync_ShouldInsertBusinessVerification()
    {
        // Arrange
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 1");
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CacVerified = true,
            CacNumber = "RC123456",
            PhoneVerified = false,
            EmailVerified = false,
            AddressVerified = false,
            IdVerified = false,
            IdVerificationStatus = "pending",
            OnlinePresenceVerified = false,
            OtherIdsVerified = false,
            BusinessDomainEmailVerified = false,
            RequiresReverification = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(verification);

        // Assert
        var retrieved = await _repository.FindByIdAsync(verification.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.BusinessId, Is.EqualTo(businessId));
        Assert.That(retrieved.CacVerified, Is.True);
        Assert.That(retrieved.CacNumber, Is.EqualTo("RC123456"));
        Assert.That(retrieved.IdVerificationStatus, Is.EqualTo("pending"));
    }

    // ========== FindByIdAsync Tests ==========

    [Test]
    public async Task FindByIdAsync_ShouldReturnVerification_WhenExists()
    {
        // Arrange
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 2");
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(verification);

        // Act
        var result = await _repository.FindByIdAsync(verification.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(verification.Id));
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
    public async Task FindByBusinessIdAsync_ShouldReturnVerification_WhenExists()
    {
        // Arrange
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 3");
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            PhoneVerified = true,
            PhoneNumber = "+1234567890",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(verification);

        // Act
        var result = await _repository.FindByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.BusinessId, Is.EqualTo(businessId));
        Assert.That(result.PhoneVerified, Is.True);
        Assert.That(result.PhoneNumber, Is.EqualTo("+1234567890"));
    }

    [Test]
    public async Task FindByBusinessIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 4");

        // Act
        var result = await _repository.FindByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.Null);
    }

    // ========== UpdateAsync Tests ==========

    [Test]
    public async Task UpdateAsync_ShouldModifyVerification()
    {
        // Arrange
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 5");
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CacVerified = false,
            PhoneVerified = false,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(verification);

        // Act
        verification.CacVerified = true;
        verification.CacNumber = "RC999999";
        verification.PhoneVerified = true;
        verification.PhoneNumber = "+9876543210";
        verification.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(verification);

        // Assert
        var updated = await _repository.FindByIdAsync(verification.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.CacVerified, Is.True);
        Assert.That(updated.CacNumber, Is.EqualTo("RC999999"));
        Assert.That(updated.PhoneVerified, Is.True);
        Assert.That(updated.PhoneNumber, Is.EqualTo("+9876543210"));
    }

    // ========== ExistsByBusinessIdAsync Tests ==========

    [Test]
    public async Task ExistsByBusinessIdAsync_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 6");
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(verification);

        // Act
        var result = await _repository.ExistsByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExistsByBusinessIdAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 7");

        // Act
        var result = await _repository.ExistsByBusinessIdAsync(businessId);

        // Assert
        Assert.That(result, Is.False);
    }

    // ========== UpdateIdVerificationStatusAsync Tests ==========

    [Test]
    public async Task UpdateIdVerificationStatusAsync_ShouldUpdateStatus()
    {
        // Arrange
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 8");
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            IdVerified = false,
            IdVerificationStatus = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(verification);

        // Act
        await _repository.UpdateIdVerificationStatusAsync(businessId, false, "pending");

        // Assert
        var updated = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.IdVerified, Is.False);
        Assert.That(updated.IdVerificationStatus, Is.EqualTo("pending"));
    }

    [Test]
    public async Task UpdateIdVerificationStatusAsync_ShouldSetVerifiedTrue()
    {
        // Arrange
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 9");
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            IdVerified = false,
            IdVerificationStatus = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(verification);

        // Act
        await _repository.UpdateIdVerificationStatusAsync(businessId, true, "approved");

        // Assert
        var updated = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.IdVerified, Is.True);
        Assert.That(updated.IdVerificationStatus, Is.EqualTo("approved"));
    }

    // ========== UpdatePhoneAndEmailVerificationAsync Tests ==========

    [Test]
    public async Task UpdatePhoneAndEmailVerificationAsync_ShouldResetVerificationStatus()
    {
        // Arrange
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 10");
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            PhoneVerified = true,
            PhoneNumber = "+1111111111",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(verification);

        // Act
        await _repository.UpdatePhoneAndEmailVerificationAsync(businessId, false, false, "+2222222222");

        // Assert
        var updated = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.PhoneVerified, Is.False);
        Assert.That(updated.EmailVerified, Is.False);
        Assert.That(updated.PhoneNumber, Is.EqualTo("+2222222222"));
    }

    [Test]
    public async Task UpdatePhoneAndEmailVerificationAsync_ShouldPreservePhoneNumber_WhenNull()
    {
        // Arrange
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 11");
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            PhoneVerified = true,
            PhoneNumber = "+3333333333",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(verification);

        // Act
        await _repository.UpdatePhoneAndEmailVerificationAsync(businessId, false, false, null);

        // Assert
        var updated = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.PhoneVerified, Is.False);
        Assert.That(updated.PhoneNumber, Is.EqualTo("+3333333333")); // Preserved
    }

    // ========== FindRequiringReverificationAsync Tests ==========

    [Test]
    public async Task FindRequiringReverificationAsync_ShouldReturnOnlyFlaggedRecords()
    {
        // Arrange
        var business1Id = await CreateTestBusiness("BusVerRepoTest Business 12a");
        var business2Id = await CreateTestBusiness("BusVerRepoTest Business 12b");
        var business3Id = await CreateTestBusiness("BusVerRepoTest Business 12c");

        var verificationRequiresReverif = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = business1Id,
            RequiresReverification = true,
            ReverificationReason = "Information changed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var verificationNoReverif = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = business2Id,
            RequiresReverification = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var verificationAlsoRequiresReverif = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = business3Id,
            RequiresReverification = true,
            ReverificationReason = "Annual review",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(verificationRequiresReverif);
        await _repository.AddAsync(verificationNoReverif);
        await _repository.AddAsync(verificationAlsoRequiresReverif);

        // Act
        var results = await _repository.FindRequiringReverificationAsync();

        // Assert
        var testResults = results.Where(r =>
            r.BusinessId == business1Id ||
            r.BusinessId == business2Id ||
            r.BusinessId == business3Id).ToList();

        Assert.That(testResults, Has.Count.EqualTo(2));
        Assert.That(testResults.All(r => r.RequiresReverification), Is.True);
    }

    // ========== Complete Verification Flow Test ==========

    [Test]
    public async Task VerificationFlow_ShouldWorkCorrectly()
    {
        // Arrange - Create business
        var businessId = await CreateTestBusiness("BusVerRepoTest Business 13");

        // Step 1: Check no verification exists
        var exists = await _repository.ExistsByBusinessIdAsync(businessId);
        Assert.That(exists, Is.False);

        // Step 2: Create verification
        var verification = new BusinessVerification
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CacVerified = false,
            PhoneVerified = false,
            EmailVerified = false,
            AddressVerified = false,
            IdVerified = false,
            IdVerificationStatus = null,
            OnlinePresenceVerified = false,
            OtherIdsVerified = false,
            BusinessDomainEmailVerified = false,
            RequiresReverification = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(verification);

        // Step 3: Check verification exists
        exists = await _repository.ExistsByBusinessIdAsync(businessId);
        Assert.That(exists, Is.True);

        // Step 4: Submit ID verification (sets pending status)
        await _repository.UpdateIdVerificationStatusAsync(businessId, false, "pending");
        var afterSubmit = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(afterSubmit!.IdVerificationStatus, Is.EqualTo("pending"));
        Assert.That(afterSubmit.IdVerified, Is.False);

        // Step 5: Approve ID verification
        await _repository.UpdateIdVerificationStatusAsync(businessId, true, "approved");
        var afterApproval = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(afterApproval!.IdVerificationStatus, Is.EqualTo("approved"));
        Assert.That(afterApproval.IdVerified, Is.True);

        // Step 6: Update phone/email (reset verification)
        await _repository.UpdatePhoneAndEmailVerificationAsync(businessId, false, false, "+1234567890");
        var afterReset = await _repository.FindByBusinessIdAsync(businessId);
        Assert.That(afterReset!.PhoneVerified, Is.False);
        Assert.That(afterReset.EmailVerified, Is.False);
    }
}
