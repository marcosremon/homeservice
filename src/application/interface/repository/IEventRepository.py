from abc import ABC, abstractmethod

from application.data_transfer_object.home_automation.sensor.presence_sensor.get_presence_sensors_status.GetPresenceSensorsStatusResponse import \
    GetPresenceSensorsStatusResponse


class IEventRepository(ABC):

    @abstractmethod
    async def GetPresenceSensorsStatus(self) -> GetPresenceSensorsStatusResponse:
        pass
