import httpx

from application.data_transfer_object.notification.SendNotification.SendNotificationRequest import SendNotificationRequest
from application.data_transfer_object.notification.SendNotification.SendNotificationResponse import SendNotificationResponse
from application.interface.service.INotificationService import INotificationService
from transversal.common.configuration.Settings import Settings
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.base.ResponseCodes import ResponseCodes

_REQUEST_TIMEOUT_SECONDS: float = 5.0

class NotificationService(INotificationService):
    """Push al movil via ntfy. El "endpoint" es la url base mas el topic: quien
    conozca el topic puede publicar y leer, asi que el topic hace de secreto y
    por eso vive en el .env y no en el codigo."""

    def __init__(self, httpClient: httpx.AsyncClient, settings: Settings):
        self._httpClient: httpx.AsyncClient = httpClient
        self._settings: Settings = settings

    # region SendNotification
    async def SendNotification(self, sendNotificationRequest: SendNotificationRequest) -> SendNotificationResponse:
        sendNotificationResponse: SendNotificationResponse = SendNotificationResponse()
        try:
            if GeneralUtils.IsNullOrEmpty(self._settings.ntfyTopic):
                # Sin topic configurado el push queda desactivado, no es un error
                # del que haya que enterarse en cada lectura del sensor.
                sendNotificationResponse.responseCode = ResponseCodes.OK
                sendNotificationResponse.isSuccess = False
                sendNotificationResponse.message = "Notifications are disabled: no ntfy topic configured."
            else:
                # El cuerpo es el texto plano del mensaje; titulo e iconos van en cabeceras.
                headers: dict[str, str] = {"Title": sendNotificationRequest.title}
                if not GeneralUtils.IsNullOrEmpty(sendNotificationRequest.tags):
                    headers["Tags"] = sendNotificationRequest.tags

                response: httpx.Response = await self._httpClient.post(
                    f"{self._settings.ntfyBaseUrl.rstrip('/')}/{self._settings.ntfyTopic}",
                    content = sendNotificationRequest.message.encode("utf-8"),
                    headers = headers,
                    timeout = _REQUEST_TIMEOUT_SECONDS,
                )

                if response.status_code != httpx.codes.OK:
                    sendNotificationResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
                    sendNotificationResponse.isSuccess = False
                    sendNotificationResponse.message = f"Ntfy responded with HTTP {response.status_code}: {response.text}"
                else:
                    sendNotificationResponse.responseCode = ResponseCodes.OK
                    sendNotificationResponse.isSuccess = True
                    sendNotificationResponse.message = "Notification sent successfully."
        except httpx.TimeoutException:
            sendNotificationResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            sendNotificationResponse.isSuccess = False
            sendNotificationResponse.message = "Timeout on NotificationService -> SendNotification"
        except Exception as ex:
            sendNotificationResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            sendNotificationResponse.isSuccess = False
            sendNotificationResponse.message = f"Unexpected error on NotificationService -> SendNotification: {ex}"

        return sendNotificationResponse
    # endregion