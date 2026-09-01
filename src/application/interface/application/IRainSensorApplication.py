from abc import ABC, abstractmethod
from application.data_transfer_object.home_automation.sensor.rain_sensor.create_rain_sensor.CreateRainSensorRequest import CreateRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.create_rain_sensor.CreateRainSensorResponse import CreateRainSensorResponse
from application.data_transfer_object.home_automation.sensor.rain_sensor.patch_rain_sensor.PatchRainSensorRequest import PatchRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.patch_rain_sensor.PatchRainSensorResponse import PatchRainSensorResponse

class IRainSensorApplication(ABC):

    @abstractmethod
    async def CreateRainSensor(self, createRainSensorRequest: CreateRainSensorRequest) -> CreateRainSensorResponse: ...

    @abstractmethod
    async def PatchRainSensor(self, patchRainSensorRequest: PatchRainSensorRequest) -> PatchRainSensorResponse: ...
