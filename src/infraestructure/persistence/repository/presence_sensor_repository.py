from datetime import timezone, datetime

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.create_presence_sensor_request import CreatePresenceSensorRequest
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.create_presence_sensor_response import CreatePresenceSensorResponse
from application.interface.repository.i_presence_sensor_repository import IPresenceSensorRepository
from domain.model.entity.device import Device
from domain.model.entity.house_zone import HouseZone
from domain.model.entity.presence_sensor import PresenceSensor
from transversal.common.utils.general_utils import GeneralUtils
from transversal.common.wrappers.base.response_codes import ResponseCodes

class PresenceSensorRepository(IPresenceSensorRepository):

    def __init__(self, session: AsyncSession):
        self._session = session

    #region create_presence_sensor
    async def create_presence_sensor(self, create_presence_sensor_request: CreatePresenceSensorRequest) -> CreatePresenceSensorResponse:
        create_presence_sensor_response: CreatePresenceSensorResponse = CreatePresenceSensorResponse()
        try:
            async with self._session.begin():
                house_zone: HouseZone | None = await self._session.scalar(select(HouseZone)
                                                                          .where(HouseZone.callout == create_presence_sensor_request.callout))
                if house_zone is None:
                    house_zone: HouseZone = HouseZone(
                        call_out = create_presence_sensor_request.callout
                    )
                    self._session.add(house_zone)
                    await self._session.flush()

                device: Device | None = await self._session.scalar(select(Device).where(
                    Device.house_zone_id == house_zone.house_zone_id,
                                Device.device_name == create_presence_sensor_request.device_name,
                                Device.device_type == create_presence_sensor_request.device_type
                            ))
                if device is None:
                    device: Device = Device(
                        house_zone_id = house_zone.house_zone_id,
                        device_name = create_presence_sensor_request.device_name,
                        device_type = create_presence_sensor_request.device_type,
                        model = create_presence_sensor_request.model,
                        manufacturer = create_presence_sensor_request.manufacturer,
                        mac_address = create_presence_sensor_request.mac_address,
                    )
                    self._session.add(device)
                    await self._session.flush()

                last_detected_presence = create_presence_sensor_request.last_detected_presence
                if last_detected_presence.tzinfo is not None:
                    last_detected_presence = last_detected_presence.astimezone(timezone.utc).replace(tzinfo=None)

                presence_sensor: PresenceSensor = PresenceSensor(
                    device_id = device.device_id,
                    ts = create_presence_sensor_request.ts,
                    presence = create_presence_sensor_request.presence,
                    distance_cm = create_presence_sensor_request.distance_cm,
                    motion = create_presence_sensor_request.motion,
                    last_detected_presence = last_detected_presence,
                )
                self._session.add(presence_sensor)

            create_presence_sensor_response.response_code = ResponseCodes.CREATED
            create_presence_sensor_response.is_success = True
            create_presence_sensor_response.message = f"Presence Sensor with the name {create_presence_sensor_request.device_name} created successfully."
        except Exception as ex:
            create_presence_sensor_response.response_code = ResponseCodes.UNEXPECTED_ERROR
            create_presence_sensor_response.is_success = False
            create_presence_sensor_response.message = f"Unexpected error on PresenceSensorRepository -> create_presence_sensor: {ex}"

        return create_presence_sensor_response
    #endregion

    #region patch_precense_sensor_data
    async def patch_presence_sensor_data(self, patch_presence_sensor_data_request: CreatePresenceSensorRequest) -> CreatePresenceSensorResponse:
        patch_presence_sensor_data_response: CreatePresenceSensorResponse = CreatePresenceSensorResponse()
        try:
            house_zone: HouseZone | None = await self._session.scalar(select(HouseZone).where(HouseZone.callout == patch_presence_sensor_data_request.callout))
            if house_zone is None:
                patch_presence_sensor_data_response.response_code = ResponseCodes.NOT_FOUND
                patch_presence_sensor_data_response.is_success = False
                patch_presence_sensor_data_response.message = "House zone not found"
            else:
                device: Device | None = await self._session.scalar(select(Device).where(Device.house_zone_id == house_zone.house_zone_id,
                                                                                        Device.device_name == patch_presence_sensor_data_request.device_name,
                                                                                        Device.device_type == patch_presence_sensor_data_request.device_type))
                if device is None:
                    patch_presence_sensor_data_response.response_code = ResponseCodes.NOT_FOUND
                    patch_presence_sensor_data_response.is_success = False
                    patch_presence_sensor_data_response.message = "Device zone not found"
                else:
                    presence_sensor: PresenceSensor | None = await self._session.scalar(select(PresenceSensor).where(PresenceSensor.device_id == device.device_id))
                    if presence_sensor is None:
                        patch_presence_sensor_data_response.response_code = ResponseCodes.NOT_FOUND
                        patch_presence_sensor_data_response.is_success = False
                        patch_presence_sensor_data_response.message = f"No presence sensor found with the name {device.device_name}"
                    else:
                        if not GeneralUtils.is_null_or_empty(patch_presence_sensor_data_request.model):
                            device.model = patch_presence_sensor_data_request.model

                        if not GeneralUtils.is_null_or_empty(patch_presence_sensor_data_request.manufacturer):
                            device.manufacturer = patch_presence_sensor_data_request.manufacturer

                        if not GeneralUtils.is_null_or_empty(patch_presence_sensor_data_request.mac_address):
                            device.mac_address = patch_presence_sensor_data_request.mac_address

                        presence_sensor.ts = patch_presence_sensor_data_request.ts
                        presence_sensor.presence = patch_presence_sensor_data_request.presence
                        presence_sensor.distance_cm = patch_presence_sensor_data_request.distance_cm
                        presence_sensor.motion = patch_presence_sensor_data_request.motion

                        if patch_presence_sensor_data_request.presence:
                            presence_sensor.last_detected_presence = datetime.utcnow()

                        await self._session.commit()

                        patch_presence_sensor_data_response.response_code = ResponseCodes.OK
                        patch_presence_sensor_data_response.is_success = True
                        patch_presence_sensor_data_response.message = f"Presence Sensor with the name {device.device_name} updated successfully."
        except Exception as ex:
            await self._session.rollback()
            patch_presence_sensor_data_response.response_code = ResponseCodes.UNEXPECTED_ERROR
            patch_presence_sensor_data_response.is_success = False
            patch_presence_sensor_data_response.message = f"Unexpected error on PresenceSensorRepository -> patch_presence_sensor: {ex}"

        return patch_presence_sensor_data_response
    #endregion