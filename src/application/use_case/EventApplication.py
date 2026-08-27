from application.data_transfer_object.home_automation.sensor.presence_sensor.get_presence_sensors_status.GetPresenceSensorsStatusResponse import GetPresenceSensorsStatusResponse
from application.interface.application.IEventApplication import IEventApplication
from application.interface.repository.IEventRepository import IEventRepository

class EventApplication(IEventApplication):

    def __init__(self, eventRepository: IEventRepository):
        self._eventRepository: IEventRepository = eventRepository

    async def GetPresenceSensorsStatus(self) -> GetPresenceSensorsStatusResponse:
        return await self._eventRepository.GetPresenceSensorsStatus()