from abc import ABC, abstractmethod

from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorRequest import CreatePresenceSensorRequest
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorResponse import CreatePresenceSensorResponse

class IPresenceSensorRepository(ABC):
    @abstractmethod
    async def CreatePresenceSensor(self, createPresenceSensorRequest: CreatePresenceSensorRequest) -> CreatePresenceSensorResponse: ...

    @abstractmethod
    async def PatchPresenceSensorData(self, patchPresenceSensorDataRequest: CreatePresenceSensorRequest) -> CreatePresenceSensorResponse: ...