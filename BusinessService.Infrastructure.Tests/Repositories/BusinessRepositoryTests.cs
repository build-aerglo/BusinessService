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
public class BusinessRepositoryTests
{
    private DapperContext _context = null!;
    private BusinessRepository _repository = null!;
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
        _repository = new BusinessRepository(_context);

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
        conn.Execute("DELETE FROM business_branches WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'BusRepoTest%');");
        conn.Execute("DELETE FROM business_category WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'BusRepoTest%');");
        conn.Execute("DELETE FROM business_verification WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'BusRepoTest%');");
        conn.Execute("DELETE FROM business_claims WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'BusRepoTest%');");
        conn.Execute("DELETE FROM business WHERE name LIKE 'BusRepoTest%';");
    }

    private async Task<Guid> CreateTestCategory(string name)
    {
        var categoryId = Guid.NewGuid();
        const string sql = @"
            INSERT INTO category (id, name, created_at, updated_at)
            VALUES (@Id, @Name, now(), now())
            ON CONFLICT (name) DO UPDATE SET name = @Name
            RETURNING id;
        ";

        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<Guid>(sql, new { Id = categoryId, Name = name });
    }

    // ========== ExistsByNameAsync Tests ==========

    [Test]
    public async Task ExistsByNameAsync_ShouldReturnTrue_WhenBusinessExists()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 1",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };
        await _repository.AddAsync(business);

        // Act
        var result = await _repository.ExistsByNameAsync("BusRepoTest Business 1");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExistsByNameAsync_ShouldReturnTrue_CaseInsensitive()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 2",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };
        await _repository.AddAsync(business);

        // Act
        var result = await _repository.ExistsByNameAsync("busrepotest business 2");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExistsByNameAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var result = await _repository.ExistsByNameAsync("NonExistent Business Name");

        // Assert
        Assert.That(result, Is.False);
    }

    // ========== AddAsync Tests ==========

    [Test]
    public async Task AddAsync_ShouldInsertBusiness()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 3",
            Website = "https://test.com",
            IsBranch = false,
            AvgRating = 4.5m,
            ReviewCount = 10,
            BusinessAddress = "123 Test St",
            BusinessEmail = "test@example.com",
            BusinessPhoneNumber = "+1234567890",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };

        // Act
        await _repository.AddAsync(business);

        // Assert
        var retrieved = await _repository.FindByIdAsync(business.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Name, Is.EqualTo("BusRepoTest Business 3"));
        Assert.That(retrieved.Website, Is.EqualTo("https://test.com"));
        Assert.That(retrieved.BusinessEmail, Is.EqualTo("test@example.com"));
    }

    [Test]
    public async Task AddAsync_ShouldInsertBusinessWithCategories()
    {
        // Arrange
        var categoryId = await CreateTestCategory("BusRepoTestCategory1");
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 4",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            Categories = new List<Category> { new() { Id = categoryId } },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };

        // Act
        await _repository.AddAsync(business);

        // Assert
        var retrieved = await _repository.FindByIdAsync(business.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Categories, Has.Count.EqualTo(1));
        Assert.That(retrieved.Categories[0].Id, Is.EqualTo(categoryId));
    }

    [Test]
    public async Task AddAsync_ShouldInsertBusinessWithIdVerificationFields()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 5",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            IdVerified = false,
            IdVerificationUrl = "https://example.com/doc.pdf",
            IdVerificationType = "CAC",
            IdVerificationNumber = "RC123456",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };

        // Act
        await _repository.AddAsync(business);

        // Assert
        var retrieved = await _repository.FindByIdAsync(business.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.IdVerified, Is.False);
        Assert.That(retrieved.IdVerificationUrl, Is.EqualTo("https://example.com/doc.pdf"));
        Assert.That(retrieved.IdVerificationType, Is.EqualTo("CAC"));
        Assert.That(retrieved.IdVerificationNumber, Is.EqualTo("RC123456"));
    }

    // ========== FindByIdAsync Tests ==========

    [Test]
    public async Task FindByIdAsync_ShouldReturnBusiness_WhenExists()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 6",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };
        await _repository.AddAsync(business);

        // Act
        var result = await _repository.FindByIdAsync(business.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(business.Id));
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

    // ========== UpdateRatingsAsync Tests ==========

    [Test]
    public async Task UpdateRatingsAsync_ShouldUpdateRatingFields()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 7",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };
        await _repository.AddAsync(business);

        // Act
        business.AvgRating = 4.75m;
        business.ReviewCount = 25;
        business.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateRatingsAsync(business);

        // Assert
        var updated = await _repository.FindByIdAsync(business.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.AvgRating, Is.EqualTo(4.75m));
        Assert.That(updated.ReviewCount, Is.EqualTo(25));
    }

    // ========== UpdateBusinessStatusAsync Tests ==========

    [Test]
    public async Task UpdateBusinessStatusAsync_ShouldUpdateStatus()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 8",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            BusinessStatus = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(business);

        // Act
        await _repository.UpdateBusinessStatusAsync(business.Id, "in_progress");

        // Assert
        var updated = await _repository.FindByIdAsync(business.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.BusinessStatus, Is.EqualTo("in_progress"));
    }

    // ========== UpdateIdVerificationAsync Tests ==========

    [Test]
    public async Task UpdateIdVerificationAsync_ShouldUpdateVerificationFields()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 9",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            IdVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };
        await _repository.AddAsync(business);

        // Act
        await _repository.UpdateIdVerificationAsync(
            business.Id,
            "https://example.com/new-doc.pdf",
            "TIN",
            "TIN-12345"
        );

        // Assert
        var updated = await _repository.FindByIdAsync(business.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.IdVerified, Is.False);
        Assert.That(updated.IdVerificationUrl, Is.EqualTo("https://example.com/new-doc.pdf"));
        Assert.That(updated.IdVerificationType, Is.EqualTo("TIN"));
        Assert.That(updated.IdVerificationNumber, Is.EqualTo("TIN-12345"));
    }

    // ========== Business Branches Tests ==========

    [Test]
    public async Task AddBusinessBranchAsync_ShouldInsertBranch()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 10",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };
        await _repository.AddAsync(business);

        var branch = new BusinessBranches
        {
            BusinessId = business.Id,
            BranchName = "Test Branch",
            BranchStreet = "456 Branch St",
            BranchCityTown = "Branch City",
            BranchState = "Branch State"
        };

        // Act
        await _repository.AddBusinessBranchAsync(branch);

        // Assert
        var branches = await _repository.GetBusinessBranchesAsync(business.Id);
        Assert.That(branches, Has.Count.EqualTo(1));
        Assert.That(branches[0]!.BranchName, Is.EqualTo("Test Branch"));
    }

    [Test]
    public async Task GetBusinessBranchesAsync_ShouldReturnOnlyActiveBranches()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 11",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };
        await _repository.AddAsync(business);

        var branch1 = new BusinessBranches
        {
            BusinessId = business.Id,
            BranchName = "Active Branch",
            BranchStreet = "789 Active St"
        };
        await _repository.AddBusinessBranchAsync(branch1);

        // Get branch ID to delete
        var branches = await _repository.GetBusinessBranchesAsync(business.Id);
        var branchId = branches[0]!.Id;

        // Add another branch and delete it
        var branch2 = new BusinessBranches
        {
            BusinessId = business.Id,
            BranchName = "Inactive Branch",
            BranchStreet = "000 Inactive St"
        };
        await _repository.AddBusinessBranchAsync(branch2);
        var allBranches = await _repository.GetBusinessBranchesAsync(business.Id);
        var inactiveBranchId = allBranches.First(b => b!.BranchName == "Inactive Branch")!.Id;
        await _repository.DeleteBusinessBranchAsync(inactiveBranchId);

        // Act
        var activeBranches = await _repository.GetBusinessBranchesAsync(business.Id);

        // Assert
        Assert.That(activeBranches, Has.Count.EqualTo(1));
        Assert.That(activeBranches[0]!.BranchName, Is.EqualTo("Active Branch"));
    }

    [Test]
    public async Task UpdateBusinessBranchAsync_ShouldModifyBranch()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 12",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };
        await _repository.AddAsync(business);

        var branch = new BusinessBranches
        {
            BusinessId = business.Id,
            BranchName = "Original Name",
            BranchStreet = "Original St"
        };
        await _repository.AddBusinessBranchAsync(branch);

        var branches = await _repository.GetBusinessBranchesAsync(business.Id);
        var branchToUpdate = branches[0]!;

        // Act
        branchToUpdate.BranchName = "Updated Name";
        branchToUpdate.BranchStreet = "Updated St";
        await _repository.UpdateBusinessBranchAsync(branchToUpdate);

        // Assert
        var updated = await _repository.FindBranchByIdAsync(branchToUpdate.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.BranchName, Is.EqualTo("Updated Name"));
        Assert.That(updated.BranchStreet, Is.EqualTo("Updated St"));
    }

    [Test]
    public async Task FindBranchByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.FindBranchByIdAsync(nonExistentId);

        // Assert
        Assert.That(result, Is.Null);
    }

    // ========== ClaimAsync Tests ==========

    [Test]
    public async Task ClaimAsync_ShouldInsertClaimAndUpdateStatus()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Business 13",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            BusinessStatus = "unclaimed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(business);

        var claim = new BusinessClaims
        {
            Id = business.Id,
            Name = "John Doe",
            Role = "Owner",
            Email = "john@example.com",
            Phone = "+1234567890"
        };

        // Act
        await _repository.ClaimAsync(claim);

        // Assert
        var updated = await _repository.FindByIdAsync(business.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.BusinessStatus, Is.EqualTo("in_progress"));
    }

    // ========== GetBranchesAsync Tests ==========

    [Test]
    public async Task GetBranchesAsync_ShouldReturnChildBusinesses()
    {
        // Arrange - Parent business
        var parentBusiness = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Parent Business",
            IsBranch = false,
            AvgRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BusinessStatus = "approved"
        };
        await _repository.AddAsync(parentBusiness);

        // Child business 1
        var childBusiness1 = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Child Business 1",
            IsBranch = true,
            ParentBusinessId = parentBusiness.Id,
            AvgRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(childBusiness1);

        // Child business 2
        var childBusiness2 = new Business
        {
            Id = Guid.NewGuid(),
            Name = "BusRepoTest Child Business 2",
            IsBranch = true,
            ParentBusinessId = parentBusiness.Id,
            AvgRating = 0,
            ReviewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(childBusiness2);

        // Act
        var branches = await _repository.GetBranchesAsync(parentBusiness.Id);

        // Assert
        Assert.That(branches, Has.Count.EqualTo(2));
        Assert.That(branches.All(b => b.ParentBusinessId == parentBusiness.Id), Is.True);
    }
}
