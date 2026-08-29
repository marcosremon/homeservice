from application.data_transfer_object.home_automation.sensor.rain_sensor.CreateRainSensor.CreateRainSensorRequest import CreateRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.CreateRainSensor.CreateRainSensorResponse import CreateRainSensorResponse
from application.data_transfer_object.home_automation.sensor.rain_sensor.PatchRainSensor.PatchRainSensorRequest import PatchRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.PatchRainSensor.PatchRainSensorResponse import PatchRainSensorResponse
from application.interface.application.IRainSensorApplication import IRainSensorApplication
from application.interface.repository.IRainSensorRepository import IRainSensorRepository

class RainSensorApplication(IRainSensorApplication):

    def __init__(self, rainSensorRepository: IRainSensorRepository):
        self._rainSensorRepository: IRainSensorRepository = rainSensorRepository

    async def CreateRainSensor(self, createRainSensorRequest: CreateRainSensorRequest) -> CreateRainSensorResponse:
        return await self._rainSensorRepository.CreateRainSensor(createRainSensorRequest)

    async def PatchRainSensor(self, patchRainSensorRequest: PatchRainSensorRequest) -> PatchRainSensorResponse:
        return await self._rainSensorRepository.PatchRainSensor(patchRainSensorRequest)
