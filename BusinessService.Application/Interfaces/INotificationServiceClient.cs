namespace BusinessService.Application.Interfaces;

public interface INotificationServiceClient
{
    Task SendNotificationAsync(NotificationRequest request);
}

public class NotificationRequest
{
    public string Template { get; set; } = default!;
    public string Channel { get; set; } = default!;
    public string Recipient { get; set; } = default!;
    public Dictionary<string, object> Payload { get; set; } = new();
}
