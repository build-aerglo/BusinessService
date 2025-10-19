namespace BusinessService.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    public List<Category> SubCategories { get; set; } = new();
    public List<Business> Businesses { get; set; } = new();
}