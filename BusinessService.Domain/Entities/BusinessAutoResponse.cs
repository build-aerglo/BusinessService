namespace BusinessService.Domain.Entities;

public class BusinessAutoResponse
{
    public Guid BusinessId { get; set; }
    public string? PositiveResponse { get; set; }
    public string? NegativeResponse { get; set; }
    public string? NeutralResponse { get; set; }
    public bool AllowAutoResponse { get; set; }
}
