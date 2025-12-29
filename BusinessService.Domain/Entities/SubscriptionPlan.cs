namespace BusinessService.Domain.Entities;

/// <summary>
/// Subscription plan definitions for businesses
/// Plans: Basic (free), Premium, Enterprise
/// </summary>
public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public SubscriptionTier Tier { get; set; }
    public string? Description { get; set; }

    // Pricing
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public string Currency { get; set; } = "NGN";

    // Limits
    public int MonthlyReplyLimit { get; set; }
    public int MonthlyDisputeLimit { get; set; }
    public int ExternalSourceLimit { get; set; }
    public int UserLoginLimit { get; set; }

    // Feature flags
    public bool PrivateReviewsEnabled { get; set; }
    public bool DataApiEnabled { get; set; }
    public bool DndModeEnabled { get; set; }
    public bool AutoResponseEnabled { get; set; }
    public bool BranchComparisonEnabled { get; set; }
    public bool CompetitorComparisonEnabled { get; set; }

    // Audit fields
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates the default Basic (free) plan
    /// </summary>
    public static SubscriptionPlan CreateBasicPlan()
    {
        return new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Basic",
            Tier = SubscriptionTier.Basic,
            Description = "Free plan with essential features",
            MonthlyPrice = 0,
            AnnualPrice = 0,
            MonthlyReplyLimit = 10,
            MonthlyDisputeLimit = 5,
            ExternalSourceLimit = 1,
            UserLoginLimit = 1,
            PrivateReviewsEnabled = false,
            DataApiEnabled = false,
            DndModeEnabled = false,
            AutoResponseEnabled = false,
            BranchComparisonEnabled = false,
            CompetitorComparisonEnabled = false
        };
    }

    /// <summary>
    /// Creates the default Premium plan
    /// </summary>
    public static SubscriptionPlan CreatePremiumPlan()
    {
        return new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Premium",
            Tier = SubscriptionTier.Premium,
            Description = "Enhanced features for growing businesses",
            MonthlyPrice = 15000,
            AnnualPrice = 150000,
            MonthlyReplyLimit = 120,
            MonthlyDisputeLimit = 25,
            ExternalSourceLimit = 3,
            UserLoginLimit = 3,
            PrivateReviewsEnabled = true,
            DataApiEnabled = false,
            DndModeEnabled = false,
            AutoResponseEnabled = false,
            BranchComparisonEnabled = false,
            CompetitorComparisonEnabled = false
        };
    }

    /// <summary>
    /// Creates the default Enterprise plan
    /// </summary>
    public static SubscriptionPlan CreateEnterprisePlan()
    {
        return new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Enterprise",
            Tier = SubscriptionTier.Enterprise,
            Description = "Full-featured plan for large businesses",
            MonthlyPrice = 50000,
            AnnualPrice = 500000,
            MonthlyReplyLimit = int.MaxValue,
            MonthlyDisputeLimit = int.MaxValue,
            ExternalSourceLimit = int.MaxValue,
            UserLoginLimit = 10,
            PrivateReviewsEnabled = true,
            DataApiEnabled = true,
            DndModeEnabled = true,
            AutoResponseEnabled = true,
            BranchComparisonEnabled = true,
            CompetitorComparisonEnabled = true
        };
    }
}

/// <summary>
/// Subscription tier levels
/// </summary>
public enum SubscriptionTier
{
    Basic = 0,
    Premium = 1,
    Enterprise = 2
}
