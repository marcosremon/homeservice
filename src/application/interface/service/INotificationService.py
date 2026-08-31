from abc import ABC, abstractmethod
from application.data_transfer_object.notification.SendNotification.SendNotificationRequest import SendNotificationRequest
from application.data_transfer_object.notification.SendNotification.SendNotificationResponse import SendNotificationResponse

class INotificationService(ABC):

    @abstractmethod
    async def SendNotification(self, sendNotificationRequest: SendNotificationRequest) -> SendNotificationResponse: ...