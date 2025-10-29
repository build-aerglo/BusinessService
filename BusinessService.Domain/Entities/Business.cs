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

    public List<Category> Categories { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

}