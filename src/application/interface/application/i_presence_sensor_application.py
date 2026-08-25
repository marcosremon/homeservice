from abc import ABC, abstractmethod

from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.create_presence_sensor_request import CreatePresenceSensorRequest
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.create_presence_sensor_response import CreatePresenceSensorResponse

class IPresenceSensorApplication(ABC):
    @abstractmethod
    async def create_presence_sensor(self, create_presence_sensor_request: CreatePresenceSensorRequest) -> CreatePresenceSensorResponse: ...