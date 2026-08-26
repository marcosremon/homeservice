from abc import ABC, abstractmethod

from application.data_transfer_object.home_automation.sensor.presence_sensor.get_presence_sensors_status.get_presence_sensors_status_response import GetPresenceSensorsStatusResponse

class IEventApplication(ABC):
    @abstractmethod
    async def get_presence_sensors_status(self) -> GetPresenceSensorsStatusResponse: ...