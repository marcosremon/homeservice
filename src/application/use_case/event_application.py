from application.data_transfer_object.home_automation.sensor.presence_sensor.get_presence_sensors_status.get_presence_sensors_status_response import GetPresenceSensorsStatusResponse
from application.interface.application.i_event_application import IEventApplication
from application.interface.repository.i_event_repository import IEventRepository

class EventApplication(IEventApplication):

    def __init__(self, event_repository: IEventRepository):
        self._event_repository = event_repository

    async def get_presence_sensors_status(self) -> GetPresenceSensorsStatusResponse:
        return await self._event_repository.get_presence_sensors_status()