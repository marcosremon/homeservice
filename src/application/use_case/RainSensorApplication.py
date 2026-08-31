from application.data_transfer_object.home_automation.sensor.rain_sensor.CreateRainSensor.CreateRainSensorRequest import CreateRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.CreateRainSensor.CreateRainSensorResponse import CreateRainSensorResponse
from application.data_transfer_object.home_automation.sensor.rain_sensor.PatchRainSensor.PatchRainSensorRequest import PatchRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.PatchRainSensor.PatchRainSensorResponse import PatchRainSensorResponse
from application.data_transfer_object.notification.SendNotification.SendNotificationRequest import SendNotificationRequest
from application.interface.application.IRainSensorApplication import IRainSensorApplication
from application.interface.repository.IRainSensorRepository import IRainSensorRepository
from application.interface.service.INotificationService import INotificationService

class RainSensorApplication(IRainSensorApplication):

    def __init__(self, rainSensorRepository: IRainSensorRepository, notificationService: INotificationService):
        self._rainSensorRepository: IRainSensorRepository = rainSensorRepository
        self._notificationService: INotificationService = notificationService

    async def CreateRainSensor(self, createRainSensorRequest: CreateRainSensorRequest) -> CreateRainSensorResponse:
        return await self._rainSensorRepository.CreateRainSensor(createRainSensorRequest)

    async def PatchRainSensor(self, patchRainSensorRequest: PatchRainSensorRequest) -> PatchRainSensorResponse:
        patchRainSensorResponse: PatchRainSensorResponse = await self._rainSensorRepository.PatchRainSensor(patchRainSensorRequest)

        if patchRainSensorResponse.isSuccess and patchRainSensorResponse.rainStarted:
            sendNotificationRequest: SendNotificationRequest = SendNotificationRequest(
                title = "Esta lloviendo",
                message = f"El sensor {patchRainSensorRequest.deviceName} de {patchRainSensorRequest.callOut} ha detectado lluvia.",
                tags = "rain_cloud",
            )

            await self._notificationService.SendNotification(sendNotificationRequest)

        return patchRainSensorResponse