namespace BusinessService.Application.DTOs.AutoResponse;

public class BusinessAutoResponseDto
{
    public Guid BusinessId { get; set; }
    public string? PositiveResponse { get; set; }
    public string? NegativeResponse { get; set; }
    public string? NeutralResponse { get; set; }
    public bool AllowAutoResponse { get; set; }
}

public class UpdateBusinessAutoResponseRequest
{
    public string? PositiveResponse { get; set; }
    public string? NegativeResponse { get; set; }
    public string? NeutralResponse { get; set; }
    public bool? AllowAutoResponse { get; set; }
}
