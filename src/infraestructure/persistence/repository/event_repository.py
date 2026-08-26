from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from application.data_transfer_object.home_automation.sensor.presence_sensor.get_presence_sensors_status.get_presence_sensors_status_response import GetPresenceSensorsStatusResponse
from application.interface.repository.i_event_repository import IEventRepository
from domain.model.entity.presence_sensor import PresenceSensor
from domain.model.entity.roomba import Roomba
from transversal.common.wrappers.base.response_codes import ResponseCodes

class EventRepository(IEventRepository):

    def __init__(self, session: AsyncSession):
        self._session = session

    # region get_presence_sensors_status
    async def get_presence_sensors_status(self) -> GetPresenceSensorsStatusResponse:
        get_presence_sensors_status_response: GetPresenceSensorsStatusResponse = GetPresenceSensorsStatusResponse()
        try:
            presence_sensors: list[PresenceSensor] = list(await self._session.scalars(select(PresenceSensor)))
            if not presence_sensors:
                get_presence_sensors_status_response.response_code = ResponseCodes.NOT_FOUND
                get_presence_sensors_status_response.is_success = False
                get_presence_sensors_status_response.message = "No presence sensors found."
            else:
                roomba: Roomba | None = await self._session.scalar(select(Roomba).limit(1))
                if roomba is None:
                    get_presence_sensors_status_response.response_code = ResponseCodes.NOT_FOUND
                    get_presence_sensors_status_response.is_success = False
                    get_presence_sensors_status_response.message = "No roomba found."
                else:
                    get_presence_sensors_status_response.is_house_empty = not any(
                        ps.presence for ps in presence_sensors)
                    get_presence_sensors_status_response.last_roomba_activation = roomba.last_roomba_activation
                    get_presence_sensors_status_response.response_code = ResponseCodes.OK
                    get_presence_sensors_status_response.is_success = True
                    get_presence_sensors_status_response.message = "Presence sensors status retrieved successfully."
        except Exception as ex:
            get_presence_sensors_status_response.response_code = ResponseCodes.UNEXPECTED_ERROR
            get_presence_sensors_status_response.is_success = False
            get_presence_sensors_status_response.message = f"Unexpected error on EventRepository -> get_presence_sensors_status: {ex}"

        return get_presence_sensors_status_response
    # endregion