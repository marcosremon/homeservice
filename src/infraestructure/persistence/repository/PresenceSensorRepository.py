from datetime import timezone, datetime
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorRequest import CreatePresenceSensorRequest
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorResponse import CreatePresenceSensorResponse
from application.interface.repository.IPresenceSensorRepository import IPresenceSensorRepository
from domain.model.entity.Device import Device
from domain.model.entity.HouseZone import HouseZone
from domain.model.entity.PresenceSensor import PresenceSensor
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.base.ResponseCodes import ResponseCodes

class PresenceSensorRepository(IPresenceSensorRepository):

    def __init__(self, session: AsyncSession):
        self._session: AsyncSession = session

    #region create_presence_sensor
    async def CreatePresenceSensor(self, createPresenceSensorRequest: CreatePresenceSensorRequest) -> CreatePresenceSensorResponse:
        createPresenceSensorResponse: CreatePresenceSensorResponse = CreatePresenceSensorResponse()
        try:
            async with self._session.begin():
                houseZone: HouseZone | None = await self._session.scalar(select(HouseZone).where(HouseZone.callout == createPresenceSensorRequest.callout))
                if houseZone is None:
                    houseZone: HouseZone = HouseZone(
                        callout = createPresenceSensorRequest.callout
                    )
                    self._session.add(houseZone)
                    await self._session.flush()

                device: Device | None = await self._session.scalar(select(Device).where(
                    Device.houseZoneId == houseZone.houseZoneId,
                                Device.deviceName == createPresenceSensorRequest.deviceName,
                                Device.deviceType == createPresenceSensorRequest.deviceType
                            ))
                if device is None:
                    device: Device = Device(
                        houseZoneId = houseZone.houseZoneId,
                        deviceName = createPresenceSensorRequest.deviceName,
                        deviceType = createPresenceSensorRequest.deviceType,
                        model = createPresenceSensorRequest.model,
                        manufacturer = createPresenceSensorRequest.manufacturer,
                        macAddress = createPresenceSensorRequest.macAddress,
                    )
                    self._session.add(device)
                    await self._session.flush()

                lastDetectedPresence: datetime = createPresenceSensorRequest.lastDetectedPresence
                if lastDetectedPresence.tzinfo is not None:
                    lastDetectedPresence = lastDetectedPresence.astimezone(timezone.utc).replace(tzinfo=None)

                presenceSensor: PresenceSensor = PresenceSensor(
                    deviceId = device.deviceId,
                    ts = createPresenceSensorRequest.ts,
                    presence = createPresenceSensorRequest.presence,
                    distanceCm = createPresenceSensorRequest.distanceCm,
                    motion = createPresenceSensorRequest.motion,
                    lastDetectedPresence = lastDetectedPresence,
                )
                self._session.add(presenceSensor)

            createPresenceSensorResponse.responseCode = ResponseCodes.CREATED
            createPresenceSensorResponse.isSuccess = True
            createPresenceSensorResponse.message = f"Presence Sensor with the name {createPresenceSensorRequest.deviceName} created successfully."
        except Exception as ex:
            createPresenceSensorResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            createPresenceSensorResponse.isSuccess = False
            createPresenceSensorResponse.message = f"Unexpected error on PresenceSensorRepository -> create_presence_sensor: {ex}"

        return createPresenceSensorResponse
    #endregion

    #region patch_precense_sensor_data
    async def PatchPresenceSensorData(self, patchPresenceSensorDataRequest: CreatePresenceSensorRequest) -> CreatePresenceSensorResponse:
        patchPresenceSensorDataResponse: CreatePresenceSensorResponse = CreatePresenceSensorResponse()
        try:
            houseZone: HouseZone | None = await self._session.scalar(select(HouseZone).where(HouseZone.callout == patchPresenceSensorDataRequest.callout))
            if houseZone is None:
                patchPresenceSensorDataResponse.responseCode = ResponseCodes.NOT_FOUND
                patchPresenceSensorDataResponse.isSuccess = False
                patchPresenceSensorDataResponse.message = "House zone not found"
            else:
                device: Device | None = await self._session.scalar(select(Device).where(Device.houseZoneId == houseZone.houseZoneId,
                                                                                        Device.deviceName == patchPresenceSensorDataRequest.deviceName,
                                                                                        Device.deviceType == patchPresenceSensorDataRequest.deviceType))
                if device is None:
                    patchPresenceSensorDataResponse.responseCode = ResponseCodes.NOT_FOUND
                    patchPresenceSensorDataResponse.isSuccess = False
                    patchPresenceSensorDataResponse.message = "Device zone not found"
                else:
                    presenceSensor: PresenceSensor | None = await self._session.scalar(select(PresenceSensor).where(PresenceSensor.deviceId == device.deviceId))
                    if presenceSensor is None:
                        patchPresenceSensorDataResponse.responseCode = ResponseCodes.NOT_FOUND
                        patchPresenceSensorDataResponse.isSuccess = False
                        patchPresenceSensorDataResponse.message = f"No presence sensor found with the name {device.deviceName}"
                    else:
                        if not GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequest.model):
                            device.model = patchPresenceSensorDataRequest.model

                        if not GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequest.manufacturer):
                            device.manufacturer = patchPresenceSensorDataRequest.manufacturer

                        if not GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequest.macAddress):
                            device.macAddress = patchPresenceSensorDataRequest.macAddress

                        presenceSensor.ts = patchPresenceSensorDataRequest.ts
                        presenceSensor.presence = patchPresenceSensorDataRequest.presence
                        presenceSensor.distanceCm = patchPresenceSensorDataRequest.distanceCm
                        presenceSensor.motion = patchPresenceSensorDataRequest.motion

                        if patchPresenceSensorDataRequest.presence:
                            presenceSensor.lastDetectedPresence = datetime.now(timezone.utc)

                        await self._session.commit()

                        patchPresenceSensorDataResponse.responseCode = ResponseCodes.OK
                        patchPresenceSensorDataResponse.isSuccess = True
                        patchPresenceSensorDataResponse.message = f"Presence Sensor with the name {device.deviceName} updated successfully."
        except Exception as ex:
            await self._session.rollback()
            patchPresenceSensorDataResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            patchPresenceSensorDataResponse.isSuccess = False
            patchPresenceSensorDataResponse.message = f"Unexpected error on PresenceSensorRepository -> patch_presence_sensor: {ex}"

        return patchPresenceSensorDataResponse
    #endregion