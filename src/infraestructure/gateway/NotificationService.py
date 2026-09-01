import httpx

from application.data_transfer_object.notification.send_notification.SendNotificationRequest import SendNotificationRequest
from application.data_transfer_object.notification.send_notification.SendNotificationResponse import SendNotificationResponse
from application.interface.service.INotificationService import INotificationService
from transversal.common.configuration.Settings import Settings
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.base.ResponseCodes import ResponseCodes

_REQUEST_TIMEOUT_SECONDS: float = 5.0

class NotificationService(INotificationService):

    def __init__(self, httpClient: httpx.AsyncClient, settings: Settings):
        self._httpClient: httpx.AsyncClient = httpClient
        self._settings: Settings = settings

    # region send_notification
    async def SendNotification(self, sendNotificationRequest: SendNotificationRequest) -> SendNotificationResponse:
        sendNotificationResponse: SendNotificationResponse = SendNotificationResponse()
        try:
            if GeneralUtils.IsNullOrEmpty(self._settings.ntfyTopic):
                sendNotificationResponse.responseCode = ResponseCodes.OK
                sendNotificationResponse.isSuccess = False
                sendNotificationResponse.message = "Notifications are disabled: no ntfy topic configured."
            else:
                headers: dict[str, str] = {"Title": sendNotificationRequest.title}
                if not GeneralUtils.IsNullOrEmpty(sendNotificationRequest.tags):
                    headers["Tags"] = sendNotificationRequest.tags

                httpxResponse: httpx.Response = await self._httpClient.post(
                    f"{self._settings.ntfyBaseUrl.rstrip('/')}/{self._settings.ntfyTopic}",
                    content = sendNotificationRequest.message.encode("utf-8"),
                    headers = headers,
                    timeout = _REQUEST_TIMEOUT_SECONDS,
                )

                if httpxResponse.status_code != httpx.codes.OK:
                    sendNotificationResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
                    sendNotificationResponse.isSuccess = False
                    sendNotificationResponse.message = f"Ntfy responded with HTTP {httpxResponse.status_code}: {httpxResponse.text}"
                else:
                    sendNotificationResponse.responseCode = ResponseCodes.OK
                    sendNotificationResponse.isSuccess = True
                    sendNotificationResponse.message = "Notification sent successfully."
        except httpx.TimeoutException:
            sendNotificationResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            sendNotificationResponse.isSuccess = False
            sendNotificationResponse.message = "Timeout on NotificationService -> send_notification"
        except Exception as ex:
            sendNotificationResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            sendNotificationResponse.isSuccess = False
            sendNotificationResponse.message = f"Unexpected error on NotificationService -> send_notification: {ex}"

        return sendNotificationResponse
    # endregion