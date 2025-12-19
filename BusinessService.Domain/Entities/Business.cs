using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessService.Domain.Entities;

public class Business
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Website { get; set; }
    public bool IsBranch { get; set; }
    public decimal AvgRating { get; set; } = 0.00m;
    public long ReviewCount { get; set; } = 0;
    public Guid? ParentBusinessId { get; set; }
    public Business? ParentBusiness { get; set; }
    public string? BusinessAddress { get; set; }
    public string? Logo { get; set; }
    [Column(TypeName = "text")]
    public string? OpeningHours { get; set; }
    public string? BusinessEmail { get; set; }
    public string? BusinessPhoneNumber { get; set; }
    public string? CacNumber { get; set; }
    public string? AccessUsername { get; set; }
    public string? AccessNumber { get; set; }
    public Dictionary<string, string>? SocialMediaLinks { get; set; }
    public string? BusinessDescription { get; set; }
    public List<string>? Media { get; set; }
    public bool IsVerified { get; set; }
    public string? ReviewLink { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string[]? Highlights { get; set; }
    public string[]? Tags { get; set; }
    public string? AverageResponseTime { get; set; }
    public long ProfileClicks { get; set; }
    public List<Faq>? Faqs { get; set; }
    [Column("qr_code_base64")]
    public string? QrCodeBase64 { get; set; }

    public List<Category> Categories { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}