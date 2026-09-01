from abc import ABC, abstractmethod
from application.data_transfer_object.notification.send_notification.SendNotificationRequest import SendNotificationRequest
from application.data_transfer_object.notification.send_notification.SendNotificationResponse import SendNotificationResponse

class INotificationService(ABC):

    @abstractmethod
    async def SendNotification(self, sendNotificationRequest: SendNotificationRequest) -> SendNotificationResponse: ...