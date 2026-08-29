from abc import ABC, abstractmethod
from application.data_transfer_object.home_automation.sensor.rain_sensor.CreateRainSensor.CreateRainSensorRequest import CreateRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.CreateRainSensor.CreateRainSensorResponse import CreateRainSensorResponse
from application.data_transfer_object.home_automation.sensor.rain_sensor.PatchRainSensor.PatchRainSensorRequest import PatchRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.PatchRainSensor.PatchRainSensorResponse import PatchRainSensorResponse

class IRainSensorRepository(ABC):

    @abstractmethod
    async def CreateRainSensor(self, createRainSensorRequest: CreateRainSensorRequest) -> CreateRainSensorResponse: ...

    @abstractmethod
    async def PatchRainSensor(self, patchRainSensorRequest: PatchRainSensorRequest) -> PatchRainSensorResponse: ...
