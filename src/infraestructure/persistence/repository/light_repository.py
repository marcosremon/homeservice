from datetime import datetime, timezone

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from application.data_transfer_object.light.get_light_by_location.get_light_by_location_request import GetLightByLocationRequest
from application.data_transfer_object.light.get_light_by_location.get_light_by_location_response import GetLightByLocationResponse
from application.data_transfer_object.light.light_dto import LightDto
from application.interface.repository.i_light_repository import ILightRepository
from domain.model.entity.device import Device
from domain.model.entity.house_zone import HouseZone
from domain.model.entity.light import Light
from domain.model.enum.Light.light_location import LightLocation
from domain.model.enum.device_type import DeviceType
from transversal.common.utils.general_utils import GeneralUtils
from transversal.common.wrappers.base.response_codes import ResponseCodes

class LightRepository(ILightRepository):

    def __init__(self, session: AsyncSession):
        self._session = session

    # region get_light_by_location
    async def get_light_by_location(self, get_light_by_location_request: GetLightByLocationRequest) -> GetLightByLocationResponse:
        get_light_by_location_response: GetLightByLocationResponse = GetLightByLocationResponse()
        try:
            location: str = str(get_light_by_location_request.location)
            light: Light | None = await self._session.scalar(select(Light).where(Light.location == location))
            if light is None:
                get_light_by_location_response.ResponseCode = ResponseCodes.NOT_FOUND
                get_light_by_location_response.IsSuccess = False
                get_light_by_location_response.Message = f"light not found"
            else:
                device: Device | None = await self._session.scalar(select(Device).where(Device.device_id == light.device_id or
                                                                                        Device.device_type == DeviceType.LIGHT.name.lower()))
                if device is None:
                    get_light_by_location_response.ResponseCode = ResponseCodes.NOT_FOUND
                    get_light_by_location_response.IsSuccess = False
                    get_light_by_location_response.Message = f"No Light found"
                else:
                    house_zone: HouseZone | None = await self._session.scalar(select(HouseZone).where(HouseZone.house_zone_id == device.house_zone_id))

                    get_light_by_location_response.response_code = ResponseCodes.OK
                    get_light_by_location_response.is_success = True
                    get_light_by_location_response.message = "Light found"
                    get_light_by_location_response.HouseZone = LightDto(
                        name = device.device_name,
                        room = house_zone.callout if house_zone is not None else "",
                        location = GeneralUtils.parse_enum(LightLocation, light.location, LightLocation.NONE),
                        mqtt_topic = light.mqtt_topic,
                        is_on = light.is_on,
                        brightness = light.brightness,
                        color = light.color,
                        color_temperature = light.color_temperature,
                        last_status_change = light.last_status_change,
                        is_online = light.is_online,
                        last_seen = light.last_seen
                    )
        except Exception as ex:
            get_light_by_location_response.response_code = ResponseCodes.UNEXPECTED_ERROR
            get_light_by_location_response.is_success = False
            get_light_by_location_response.message = f"Unexpected error on EventRepository -> get_presence_sensors_status: {ex}"

        return get_light_by_location_response
    # endregion

    # region patch_light_status
    async def patch_light_status(self, light_location: LightLocation, is_on: bool) -> None:
        try:
            location: str = str(light_location)
            light: Light | None= await self._session.scalar(select(Light).where(Light.location == location))
            if light is None:
                print(f"ERROR light {light_location} not found")
            elif light.is_on == is_on:
                light.is_on = is_on
                light.last_updated_at = datetime.now(timezone.utc)
                print(f"light {light_location} IsOn cambiado a {is_on}")
        except Exception as ex:
            print(f"Unexpected error on LightRepository -> PatchLightStatus {ex}")
    # endregion