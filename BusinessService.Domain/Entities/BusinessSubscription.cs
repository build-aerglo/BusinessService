namespace BusinessService.Domain.Entities;

/// <summary>
/// Business subscription tracking with usage limits
/// </summary>
public class BusinessSubscription
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid SubscriptionPlanId { get; set; }

    // Subscription period
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? BillingDate { get; set; }
    public bool IsAnnual { get; set; }

    // Status
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Monthly usage tracking (resets on billing date)
    public int RepliesUsedThisMonth { get; set; }
    public int DisputesUsedThisMonth { get; set; }
    public DateTime UsageResetDate { get; set; }

    // Navigation
    public SubscriptionPlan? Plan { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Checks if subscription is currently active
    /// </summary>
    public bool IsActive => Status == SubscriptionStatus.Active && DateTime.UtcNow < EndDate;

    /// <summary>
    /// Checks if reply limit has been reached
    /// </summary>
    public bool CanReply(int monthlyLimit)
    {
        CheckAndResetUsage();
        return monthlyLimit == int.MaxValue || RepliesUsedThisMonth < monthlyLimit;
    }

    /// <summary>
    /// Checks if dispute limit has been reached
    /// </summary>
    public bool CanDispute(int monthlyLimit)
    {
        CheckAndResetUsage();
        return monthlyLimit == int.MaxValue || DisputesUsedThisMonth < monthlyLimit;
    }

    /// <summary>
    /// Increments reply usage count
    /// </summary>
    public void IncrementReplyUsage()
    {
        CheckAndResetUsage();
        RepliesUsedThisMonth++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments dispute usage count
    /// </summary>
    public void IncrementDisputeUsage()
    {
        CheckAndResetUsage();
        DisputesUsedThisMonth++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets usage counters if billing period has passed
    /// </summary>
    private void CheckAndResetUsage()
    {
        if (DateTime.UtcNow >= UsageResetDate)
        {
            RepliesUsedThisMonth = 0;
            DisputesUsedThisMonth = 0;
            UsageResetDate = CalculateNextResetDate();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    private DateTime CalculateNextResetDate()
    {
        return UsageResetDate.AddMonths(1);
    }

    /// <summary>
    /// Cancels the subscription
    /// </summary>
    public void Cancel(string? reason = null)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Suspends the subscription
    /// </summary>
    public void Suspend()
    {
        Status = SubscriptionStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates a suspended subscription
    /// </summary>
    public void Reactivate()
    {
        if (Status == SubscriptionStatus.Suspended)
        {
            Status = SubscriptionStatus.Active;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

/// <summary>
/// Subscription status options
/// </summary>
public enum SubscriptionStatus
{
    Active = 0,
    Suspended = 1,
    Cancelled = 2,
    Expired = 3,
    PendingPayment = 4
}
