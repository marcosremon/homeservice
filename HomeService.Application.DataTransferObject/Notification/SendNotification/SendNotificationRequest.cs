namespace HomeService.Application.DataTransferObject.Notification.SendNotification;

public class SendNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
}
