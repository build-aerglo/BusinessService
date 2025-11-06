namespace BusinessService.Application.Interfaces;

/// <summary>
/// Client for communicating with UserService to get business rep information
/// This is needed because BusinessRepRepository lives in UserService
/// </summary>
public interface IBusinessRepServiceClient
{
    /// <summary>
    /// Gets the parent business rep for a business (first rep created)
    /// </summary>
    Task<BusinessRepDto?> GetParentRepByBusinessIdAsync(Guid businessId);
    
    /// <summary>
    /// Gets a business rep by ID
    /// </summary>
    Task<BusinessRepDto?> GetBusinessRepByIdAsync(Guid businessRepId);
}

/// <summary>
/// Business rep DTO returned from UserService
/// </summary>
public record BusinessRepDto(
    Guid Id,
    Guid BusinessId,
    Guid UserId,
    string? BranchName,
    string? BranchAddress,
    DateTime CreatedAt
);