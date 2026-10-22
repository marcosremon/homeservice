using HomeService.Application.DataTransferObject.Notification.SendNotification;

namespace HomeService.Application.Interface.Service;

public interface INotificationService
{
    Task<SendNotificationResponse> SendNotification(SendNotificationRequest sendNotificationRequest);
}
