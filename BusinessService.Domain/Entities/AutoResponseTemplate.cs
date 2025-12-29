namespace BusinessService.Domain.Entities;

/// <summary>
/// Auto-response templates for automated review responses (Enterprise feature)
/// Templates are selected based on review sentiment (Positive/Negative/Neutral)
/// </summary>
public class AutoResponseTemplate
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }

    // Template details
    public string Name { get; set; } = default!;
    public ReviewSentiment Sentiment { get; set; }
    public string TemplateContent { get; set; } = default!;

    // Template settings
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public int Priority { get; set; }

    // Star rating filter (optional)
    public int? MinStarRating { get; set; }
    public int? MaxStarRating { get; set; }

    // Usage statistics
    public int TimesUsed { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// Populates template variables with actual values
    /// Supported variables: {reviewer_name}, {business_name}, {star_rating}
    /// </summary>
    public string PopulateTemplate(string reviewerName, string businessName, int starRating)
    {
        var content = TemplateContent;
        content = content.Replace("{reviewer_name}", reviewerName);
        content = content.Replace("{business_name}", businessName);
        content = content.Replace("{star_rating}", starRating.ToString());
        return content;
    }

    /// <summary>
    /// Checks if template matches the review criteria
    /// </summary>
    public bool MatchesReview(ReviewSentiment sentiment, int starRating)
    {
        if (!IsActive) return false;
        if (Sentiment != sentiment) return false;

        if (MinStarRating.HasValue && starRating < MinStarRating.Value)
            return false;
        if (MaxStarRating.HasValue && starRating > MaxStarRating.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Records template usage
    /// </summary>
    public void RecordUsage()
    {
        TimesUsed++;
        LastUsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a default positive template
    /// </summary>
    public static AutoResponseTemplate CreateDefaultPositive(Guid businessId)
    {
        return new AutoResponseTemplate
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Default Positive Response",
            Sentiment = ReviewSentiment.Positive,
            TemplateContent = "Thank you so much for your wonderful review, {reviewer_name}! We're thrilled to hear you had a great experience at {business_name}. Your {star_rating}-star rating means a lot to us, and we look forward to serving you again soon!",
            IsDefault = true,
            IsActive = true,
            Priority = 1
        };
    }

    /// <summary>
    /// Creates a default negative template
    /// </summary>
    public static AutoResponseTemplate CreateDefaultNegative(Guid businessId)
    {
        return new AutoResponseTemplate
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Default Negative Response",
            Sentiment = ReviewSentiment.Negative,
            TemplateContent = "Dear {reviewer_name}, thank you for taking the time to share your feedback. We're sorry to hear your experience at {business_name} didn't meet your expectations. We take all feedback seriously and would love the opportunity to make things right. Please reach out to us directly so we can address your concerns.",
            IsDefault = true,
            IsActive = true,
            Priority = 1
        };
    }

    /// <summary>
    /// Creates a default neutral template
    /// </summary>
    public static AutoResponseTemplate CreateDefaultNeutral(Guid businessId)
    {
        return new AutoResponseTemplate
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Default Neutral Response",
            Sentiment = ReviewSentiment.Neutral,
            TemplateContent = "Thank you for your feedback, {reviewer_name}! We appreciate you taking the time to review {business_name}. Your {star_rating}-star rating and comments help us improve. If there's anything we can do better, please let us know!",
            IsDefault = true,
            IsActive = true,
            Priority = 1
        };
    }
}

/// <summary>
/// Review sentiment classification
/// </summary>
public enum ReviewSentiment
{
    Positive = 0,
    Neutral = 1,
    Negative = 2
}
