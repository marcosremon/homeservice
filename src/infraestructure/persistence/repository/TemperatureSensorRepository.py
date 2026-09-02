from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from application.data_transfer_object.home_automation.sensor.temperature_sensor.create_temperature_sensor.CreateTemperatureSensorRequest import CreateTemperatureSensorRequest
from application.data_transfer_object.home_automation.sensor.temperature_sensor.create_temperature_sensor.CreateTemperatureSensorResponse import CreateTemperatureSensorResponse
from application.data_transfer_object.home_automation.sensor.temperature_sensor.patch_temperature_sensor.PatchTemperatureSensorRequest import PatchTemperatureSensorRequest
from application.data_transfer_object.home_automation.sensor.temperature_sensor.patch_temperature_sensor.PatchTemperatureSensorResponse import PatchTemperatureSensorResponse
from application.interface.repository.ITemperatureSensorRepository import ITemperatureSensorRepository
from domain.model.entity.HouseZone import HouseZone
from domain.model.entity.TemperatureSensor import TemperatureSensor
from domain.model.entity.Device import Device
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.base.ResponseCodes import ResponseCodes

class TemperatureSensorRepository(ITemperatureSensorRepository):

    def __init__(self, session: AsyncSession):
        self._session: AsyncSession = session

    #region CreateTemperatureSensor
    async def CreateTemperatureSensor(self, createTemperatureSensorRequest: CreateTemperatureSensorRequest) -> CreateTemperatureSensorResponse:
        createTemperatureSensorResponse: CreateTemperatureSensorResponse = CreateTemperatureSensorResponse()
        try:
            async with self._session.begin():
                foundHouseZone: HouseZone | None = await self._session.scalar(
                    select(HouseZone).where(HouseZone.callout == createTemperatureSensorRequest.callOut))

                if foundHouseZone is not None:
                    houseZone: HouseZone = foundHouseZone
                else:
                    houseZone = HouseZone(
                        callout = createTemperatureSensorRequest.callOut
                    )
                    self._session.add(houseZone)
                    await self._session.flush()

                device: Device | None = await self._session.scalar(select(Device)
                    .where(Device.houseZoneId == houseZone.houseZoneId,
                           Device.deviceName == createTemperatureSensorRequest.deviceName,
                           Device.deviceType == createTemperatureSensorRequest.deviceType))
                if device is not None:
                    createTemperatureSensorResponse.responseCode = ResponseCodes.CONFLICT
                    createTemperatureSensorResponse.isSuccess = False
                    createTemperatureSensorResponse.message = f"the device with macAddress {createTemperatureSensorRequest.macAddress} and device name {createTemperatureSensorRequest.deviceName} already exists"
                else:
                    device: Device = Device(
                        houseZoneId = houseZone.houseZoneId,
                        deviceName = createTemperatureSensorRequest.deviceName,
                        deviceType = createTemperatureSensorRequest.deviceType,
                        model = createTemperatureSensorRequest.model,
                        manufacturer = createTemperatureSensorRequest.manufacturer,
                        macAddress = createTemperatureSensorRequest.macAddress,
                    )

                    self._session.add(device)
                    await self._session.flush()

                    temperatureSensor: TemperatureSensor = TemperatureSensor(
                        deviceId = device.deviceId,
                        temperature = createTemperatureSensorRequest.temperature,
                        adcVoltage = createTemperatureSensorRequest.adcVoltage,
                        measureAt = createTemperatureSensorRequest.measureAt,
                    )

                    self._session.add(temperatureSensor)
                    await self._session.flush()

                    createTemperatureSensorResponse.responseCode = ResponseCodes.CREATED
                    createTemperatureSensorResponse.isSuccess = True
                    createTemperatureSensorResponse.message = f"Temperature Sensor with the name {createTemperatureSensorRequest.deviceName} created successfully."
        except Exception as ex:
            createTemperatureSensorResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            createTemperatureSensorResponse.isSuccess = False
            createTemperatureSensorResponse.message = f"Unexpected error on TemperatureSensorRepository -> createTemperatureSensorResponse: {ex}"

        return createTemperatureSensorResponse
    #endregion

    #region PatchTemperatureSensor
    async def PatchTemperatureSensor(self, patchTemperatureSensorRequest: PatchTemperatureSensorRequest) -> PatchTemperatureSensorResponse:
        patchTemperatureSensorResponse: PatchTemperatureSensorResponse = PatchTemperatureSensorResponse()
        try:
            houseZone: HouseZone | None = await self._session.scalar(select(HouseZone)
                .where(HouseZone.callout == patchTemperatureSensorRequest.callOut))
            if houseZone is None:
                patchTemperatureSensorResponse.responseCode = ResponseCodes.NOT_FOUND
                patchTemperatureSensorResponse.isSuccess = False
                patchTemperatureSensorResponse.message = f"House zone {patchTemperatureSensorRequest.callOut} not found"
            else:
                device: Device | None = await self._session.scalar(select(Device)
                    .where(Device.houseZoneId == houseZone.houseZoneId,
                           Device.deviceName == patchTemperatureSensorRequest.deviceName,
                           Device.deviceType == patchTemperatureSensorRequest.deviceType))
                if device is None:
                    patchTemperatureSensorResponse.responseCode = ResponseCodes.NOT_FOUND
                    patchTemperatureSensorResponse.isSuccess = False
                    patchTemperatureSensorResponse.message = f"Device {patchTemperatureSensorRequest.deviceName} not found"
                else:
                    temperatureSensor: TemperatureSensor | None = await self._session.scalar(select(TemperatureSensor)
                        .where(TemperatureSensor.deviceId == device.deviceId))
                    if temperatureSensor is None:
                        patchTemperatureSensorResponse.responseCode = ResponseCodes.NOT_FOUND
                        patchTemperatureSensorResponse.isSuccess = False
                        patchTemperatureSensorResponse.message = f"No temperature sensor found with the name {device.deviceName}"
                    else:
                        if not GeneralUtils.IsNullOrEmpty(patchTemperatureSensorRequest.model):
                            device.model = patchTemperatureSensorRequest.model

                        if not GeneralUtils.IsNullOrEmpty(patchTemperatureSensorRequest.macAddress):
                            device.macAddress = patchTemperatureSensorRequest.macAddress

                        if patchTemperatureSensorRequest.temperature is not None:
                            temperatureSensor.temperature = patchTemperatureSensorRequest.temperature

                        if patchTemperatureSensorRequest.adcVoltage is not None:
                            temperatureSensor.adcVoltage = patchTemperatureSensorRequest.adcVoltage

                        if patchTemperatureSensorRequest.measureAt is not None:
                            temperatureSensor.measureAt = patchTemperatureSensorRequest.measureAt

                        await self._session.commit()

                        patchTemperatureSensorResponse.responseCode = ResponseCodes.OK
                        patchTemperatureSensorResponse.isSuccess = True
                        patchTemperatureSensorResponse.message = f"Temperature Sensor with the name {device.deviceName} updated successfully."
        except Exception as ex:
            await self._session.rollback()
            patchTemperatureSensorResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            patchTemperatureSensorResponse.isSuccess = False
            patchTemperatureSensorResponse.message = f"Unexpected error on TemperatureSensorRepository -> PatchTemperatureSensor: {ex}"

        return patchTemperatureSensorResponse
    #endregion