from abc import ABC, abstractmethod
from application.data_transfer_object.home_automation.sensor.temperature_sensor.CreateTemperatureSensor.CreateTemperatureSensorRequest import CreateTemperatureSensorRequest
from application.data_transfer_object.home_automation.sensor.temperature_sensor.CreateTemperatureSensor.CreateTemperatureSensorResponse import CreateTemperatureSensorResponse

class ITemperatureSensorApplication(ABC):

    @abstractmethod
    async def CreateTemperatureSensor(self, createTemperatureSensorRequest: CreateTemperatureSensorRequest) -> CreateTemperatureSensorResponse: ...
