namespace BusinessService.Domain.Exceptions;

public class SubscriptionNotFoundException : Exception
{
    public SubscriptionNotFoundException()
        : base("Subscription not found.") { }

    public SubscriptionNotFoundException(string message)
        : base(message) { }

    public SubscriptionNotFoundException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class SubscriptionLimitExceededException : Exception
{
    public string LimitType { get; }
    public int CurrentUsage { get; }
    public int MaxLimit { get; }

    public SubscriptionLimitExceededException(string limitType, int currentUsage, int maxLimit)
        : base($"{limitType} limit exceeded. Current usage: {currentUsage}, Max limit: {maxLimit}")
    {
        LimitType = limitType;
        CurrentUsage = currentUsage;
        MaxLimit = maxLimit;
    }

    public SubscriptionLimitExceededException(string message)
        : base(message)
    {
        LimitType = "Unknown";
        CurrentUsage = 0;
        MaxLimit = 0;
    }
}

public class FeatureNotAvailableException : Exception
{
    public string FeatureName { get; }
    public string RequiredPlan { get; }

    public FeatureNotAvailableException(string featureName, string requiredPlan)
        : base($"Feature '{featureName}' is not available on your current plan. Upgrade to {requiredPlan} to access this feature.")
    {
        FeatureName = featureName;
        RequiredPlan = requiredPlan;
    }

    public FeatureNotAvailableException(string message)
        : base(message)
    {
        FeatureName = "Unknown";
        RequiredPlan = "Unknown";
    }
}

public class SubscriptionExpiredException : Exception
{
    public SubscriptionExpiredException()
        : base("Subscription has expired. Please renew to continue using this feature.") { }

    public SubscriptionExpiredException(string message)
        : base(message) { }
}

public class InvalidSubscriptionOperationException : Exception
{
    public InvalidSubscriptionOperationException()
        : base("Invalid subscription operation.") { }

    public InvalidSubscriptionOperationException(string message)
        : base(message) { }

    public InvalidSubscriptionOperationException(string message, Exception? innerException)
        : base(message, innerException) { }
}
