from datetime import datetime, timezone

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from application.data_transfer_object.light.get_light_by_location.GetLightByLocationRequest import GetLightByLocationRequest
from application.data_transfer_object.light.get_light_by_location.GetLightByLocationResponse import GetLightByLocationResponse
from application.data_transfer_object.light.LightDto import LightDto
from application.interface.repository.ILightRepository import ILightRepository
from domain.model.entity.Device import Device
from domain.model.entity.HouseZone import HouseZone
from domain.model.entity.Light import Light
from domain.model.enum.Light.LightLocation import LightLocation
from domain.model.enum.DeviceType import DeviceType
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.base.ResponseCodes import ResponseCodes

class LightRepository(ILightRepository):

    def __init__(self, session: AsyncSession):
        self._session = session

    # region get_light_by_location
    async def GetLightByLocation(self, getLightByLocationRequest: GetLightByLocationRequest) -> GetLightByLocationResponse:
        getLightByLocationResponse: GetLightByLocationResponse = GetLightByLocationResponse()
        try:
            location: str = getLightByLocationRequest.location.name
            light: Light | None = await self._session.scalar(select(Light).where(Light.location == location))
            if light is None:
                getLightByLocationResponse.ResponseCode = ResponseCodes.NOT_FOUND
                getLightByLocationResponse.IsSuccess = False
                getLightByLocationResponse.Message = f"light not found"
            else:
                device: Device | None = await self._session.scalar(select(Device).where(Device.deviceId == light.deviceId or
                                                                                        Device.deviceType == DeviceType.LIGHT.name.lower()))
                if device is None:
                    getLightByLocationResponse.ResponseCode = ResponseCodes.NOT_FOUND
                    getLightByLocationResponse.IsSuccess = False
                    getLightByLocationResponse.Message = f"No Light found"
                else:
                    houseZone: HouseZone | None = await self._session.scalar(select(HouseZone).where(HouseZone.houseZoneId == device.houseZoneId))

                    getLightByLocationResponse.responseCode = ResponseCodes.OK
                    getLightByLocationResponse.isSuccess = True
                    getLightByLocationResponse.message = "Light found"
                    getLightByLocationResponse.HouseZone = LightDto(
                        name = device.deviceName,
                        room = houseZone.callout if houseZone is not None else "",
                        location = GeneralUtils.ParseEnum(LightLocation, light.location, LightLocation.NONE),
                        mqttTopic = light.mqttTopic,
                        isOn = light.isOn,
                        brightness = light.brightness,
                        color = light.color,
                        colorTemperature = light.colorTemperature,
                        lastStatusChange = light.lastStatusChange,
                        isOnline = light.isOnline,
                        lastSeen = light.lastSeen
                    )
        except Exception as ex:
            getLightByLocationResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            getLightByLocationResponse.isSuccess = False
            getLightByLocationResponse.message = f"Unexpected error on EventRepository -> get_presence_sensors_status: {ex}"

        return getLightByLocationResponse
    # endregion

    # region patch_light_status
    async def PatchLightStatus(self, lightLocation: LightLocation, isOn: bool) -> None:
        try:
            location: str = lightLocation.name
            light: Light | None = await self._session.scalar(select(Light).where(Light.location == location))
            if light is None:
                print(f"ERROR light {lightLocation.name} not found")
            elif light.isOn == isOn:
                light.isOn = isOn
                light.lastUpdatedAt = datetime.now(timezone.utc)
                print(f"light {lightLocation.name} IsOn cambiado a {isOn}")
        except Exception as ex:
            print(f"Unexpected error on LightRepository -> PatchLightStatus {ex}")
    # endregion