namespace BusinessService.Domain.Entities;

public class Tags
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; } = default!;
    public string Name { get; set; } = default!;
}