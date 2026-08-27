from abc import ABC, abstractmethod
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorRequest import CreatePresenceSensorRequest
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorResponse import CreatePresenceSensorResponse
from application.data_transfer_object.home_automation.sensor.presence_sensor.patch_presence_sensor_data.PatchPresenceSensorDataRequest import PatchPresenceSensorDataRequest
from application.data_transfer_object.home_automation.sensor.presence_sensor.patch_presence_sensor_data.PatchPresenceSensorDataResponse import PatchPresenceSensorDataResponse

class IPresenceSensorApplication(ABC):

    @abstractmethod
    async def CreatePresenceSensor(self, createPresenceSensorRequest: CreatePresenceSensorRequest) -> CreatePresenceSensorResponse: ...

    @abstractmethod
    async def PatchPresenceSensorData(self, patchPresenceSensorDataRequest: PatchPresenceSensorDataRequest) -> PatchPresenceSensorDataResponse: ...