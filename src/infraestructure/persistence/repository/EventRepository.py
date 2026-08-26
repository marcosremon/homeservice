from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from application.data_transfer_object.home_automation.sensor.presence_sensor.get_presence_sensors_status.GetPresenceSensorsStatusResponse import GetPresenceSensorsStatusResponse
from application.interface.repository.IEventRepository import IEventRepository
from domain.model.entity.PresenceSensor import PresenceSensor
from domain.model.entity.Roomba import Roomba
from transversal.common.wrappers.base.ResponseCodes import ResponseCodes

class EventRepository(IEventRepository):

    def __init__(self, session: AsyncSession):
        self._session = session

    # region get_presence_sensors_status
    async def GetPresenceSensorsStatus(self) -> GetPresenceSensorsStatusResponse:
        getPresenceSensorsStatusResponse: GetPresenceSensorsStatusResponse = GetPresenceSensorsStatusResponse()
        try:
            presenceSensors: list[PresenceSensor] = list(await self._session.scalars(select(PresenceSensor)))
            if not presenceSensors:
                getPresenceSensorsStatusResponse.responseCode = ResponseCodes.NOT_FOUND
                getPresenceSensorsStatusResponse.isSuccess = False
                getPresenceSensorsStatusResponse.message = "No presence sensors found."
            else:
                roomba: Roomba | None = await self._session.scalar(select(Roomba).limit(1))
                if roomba is None:
                    getPresenceSensorsStatusResponse.responseCode = ResponseCodes.NOT_FOUND
                    getPresenceSensorsStatusResponse.isSuccess = False
                    getPresenceSensorsStatusResponse.message = "No roomba found."
                else:
                    getPresenceSensorsStatusResponse.isHouseEmpty = not any(
                        ps.presence for ps in presenceSensors)
                    getPresenceSensorsStatusResponse.lastRoombaActivation = roomba.lastRoombaActivation
                    getPresenceSensorsStatusResponse.responseCode = ResponseCodes.OK
                    getPresenceSensorsStatusResponse.isSuccess = True
                    getPresenceSensorsStatusResponse.message = "Presence sensors status retrieved successfully."
        except Exception as ex:
            getPresenceSensorsStatusResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            getPresenceSensorsStatusResponse.isSuccess = False
            getPresenceSensorsStatusResponse.message = f"Unexpected error on EventRepository -> get_presence_sensors_status: {ex}"

        return getPresenceSensorsStatusResponse
    # endregion