from datetime import datetime
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorRequest import CreatePresenceSensorRequest
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorResponse import CreatePresenceSensorResponse
from application.data_transfer_object.home_automation.sensor.presence_sensor.get_presence_sensors_status.GetPresenceSensorsStatusResponse import \
    GetPresenceSensorsStatusResponse
from application.interface.repository.IPresenceSensorRepository import IPresenceSensorRepository
from domain.model.entity.Device import Device
from domain.model.entity.HouseZone import HouseZone
from domain.model.entity.PresenceSensor import PresenceSensor
from domain.model.entity.Roomba import Roomba
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.base.ResponseCodes import ResponseCodes

class PresenceSensorRepository(IPresenceSensorRepository):

    def __init__(self, session: AsyncSession):
        self._session: AsyncSession = session

    #region CreatePresenceSensor
    async def CreatePresenceSensor(self, createPresenceSensorRequest: CreatePresenceSensorRequest) -> CreatePresenceSensorResponse:
        createPresenceSensorResponse: CreatePresenceSensorResponse = CreatePresenceSensorResponse()
        try:
            async with self._session.begin():
                foundHouseZone: HouseZone | None = await self._session.scalar(select(HouseZone).where(HouseZone.callout == createPresenceSensorRequest.callOut))

                if foundHouseZone is not None:
                    houseZone: HouseZone = foundHouseZone
                else:
                    houseZone = HouseZone(
                        callout = createPresenceSensorRequest.callOut
                    )
                    self._session.add(houseZone)
                    await self._session.flush()

                foundDevice: Device | None = await self._session.scalar(select(Device).where(
                    Device.houseZoneId == houseZone.houseZoneId,
                    Device.deviceName == createPresenceSensorRequest.deviceName,
                    Device.deviceType == createPresenceSensorRequest.deviceType))

                if foundDevice is not None:
                    device: Device = foundDevice
                else:
                    device = Device(
                        houseZoneId = houseZone.houseZoneId,
                        deviceName = createPresenceSensorRequest.deviceName,
                        deviceType = createPresenceSensorRequest.deviceType,
                        model = createPresenceSensorRequest.model,
                        manufacturer = createPresenceSensorRequest.manufacturer,
                        macAddress = createPresenceSensorRequest.macAddress,
                    )
                    self._session.add(device)
                    await self._session.flush()

                lastDetectedPresence: datetime = GeneralUtils.UtcNow() if createPresenceSensorRequest.presence else datetime.min

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

    #region PatchPresenceSensorData
    async def PatchPresenceSensorData(self, patchPresenceSensorDataRequest: CreatePresenceSensorRequest) -> CreatePresenceSensorResponse:
        patchPresenceSensorDataResponse: CreatePresenceSensorResponse = CreatePresenceSensorResponse()
        try:
            houseZone: HouseZone | None = await self._session.scalar(select(HouseZone).where(HouseZone.callout == patchPresenceSensorDataRequest.callOut))
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
                            presenceSensor.lastDetectedPresence = GeneralUtils.UtcNow()

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

    # region GetPresenceSensorsStatus
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
                    getPresenceSensorsStatusResponse.isHouseEmpty = not any(ps.presence for ps in presenceSensors)
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