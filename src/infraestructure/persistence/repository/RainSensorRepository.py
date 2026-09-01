from datetime import datetime
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from application.data_transfer_object.home_automation.sensor.rain_sensor.create_rain_sensor.CreateRainSensorRequest import CreateRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.create_rain_sensor.CreateRainSensorResponse import CreateRainSensorResponse
from application.data_transfer_object.home_automation.sensor.rain_sensor.get_raining_sensor.GetRainingSensorResponse import \
    GetRainingSensorResponse
from application.data_transfer_object.home_automation.sensor.rain_sensor.patch_rain_sensor.PatchRainSensorRequest import PatchRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.patch_rain_sensor.PatchRainSensorResponse import PatchRainSensorResponse
from application.interface.repository.IRainSensorRepository import IRainSensorRepository
from domain.model.entity.Device import Device
from domain.model.entity.HouseZone import HouseZone
from domain.model.entity.RainSensor import RainSensor
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.base.ResponseCodes import ResponseCodes

class RainSensorRepository(IRainSensorRepository):

    def __init__(self, session: AsyncSession):
        self._session: AsyncSession = session

    #region create_rain_sensor
    async def CreateRainSensor(self, createRainSensorRequest: CreateRainSensorRequest) -> CreateRainSensorResponse:
        createRainSensorResponse: CreateRainSensorResponse = CreateRainSensorResponse()
        try:
            async with self._session.begin():
                foundHouseZone: HouseZone | None = await self._session.scalar(
                    select(HouseZone).where(HouseZone.callout == createRainSensorRequest.callOut))

                if foundHouseZone is not None:
                    houseZone: HouseZone = foundHouseZone
                else:
                    houseZone = HouseZone(
                        callout = createRainSensorRequest.callOut
                    )
                    self._session.add(houseZone)
                    await self._session.flush()

                device: Device | None = await self._session.scalar(select(Device)
                    .where(Device.houseZoneId == houseZone.houseZoneId,
                           Device.deviceName == createRainSensorRequest.deviceName,
                           Device.deviceType == createRainSensorRequest.deviceType))
                if device is not None:
                    createRainSensorResponse.responseCode = ResponseCodes.CONFLICT
                    createRainSensorResponse.isSuccess = False
                    createRainSensorResponse.message = f"the device with macAddress {createRainSensorRequest.macAddress} and device name {createRainSensorRequest.deviceName} already exists"
                else:
                    device: Device = Device(
                        houseZoneId = houseZone.houseZoneId,
                        deviceName = createRainSensorRequest.deviceName,
                        deviceType = createRainSensorRequest.deviceType,
                        model = createRainSensorRequest.model,
                        manufacturer = createRainSensorRequest.manufacturer,
                        macAddress = createRainSensorRequest.macAddress,
                    )

                    self._session.add(device)
                    await self._session.flush()

                    measureAt: datetime = GeneralUtils.UtcNow()

                    rainSensor: RainSensor = RainSensor(
                        deviceId = device.deviceId,
                        adcValue = createRainSensorRequest.adcValue,
                        wetnessPercent = createRainSensorRequest.wetnessPercent,
                        isRaining = createRainSensorRequest.isRaining,
                        measureAt = measureAt,
                        lastDetectedRain = measureAt if createRainSensorRequest.isRaining else datetime.min
                    )

                    self._session.add(rainSensor)
                    await self._session.flush()

                    createRainSensorResponse.responseCode = ResponseCodes.CREATED
                    createRainSensorResponse.isSuccess = True
                    createRainSensorResponse.message = f"Rain Sensor with the name {createRainSensorRequest.deviceName} created successfully."
        except Exception as ex:
            createRainSensorResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            createRainSensorResponse.isSuccess = False
            createRainSensorResponse.message = f"Unexpected error on RainSensorRepository -> CreateRainSensor: {ex}"

        return createRainSensorResponse
    #endregion

    #region patch_rain_sensor
    async def PatchRainSensor(self, patchRainSensorRequest: PatchRainSensorRequest) -> PatchRainSensorResponse:
        patchRainSensorResponse: PatchRainSensorResponse = PatchRainSensorResponse()
        try:
            houseZone: HouseZone | None = await self._session.scalar(select(HouseZone)
                .where(HouseZone.callout == patchRainSensorRequest.callOut))
            if houseZone is None:
                patchRainSensorResponse.responseCode = ResponseCodes.NOT_FOUND
                patchRainSensorResponse.isSuccess = False
                patchRainSensorResponse.message = f"House zone {patchRainSensorRequest.callOut} not found"
            else:
                device: Device | None = await self._session.scalar(select(Device)
                    .where(Device.houseZoneId == houseZone.houseZoneId,
                           Device.deviceName == patchRainSensorRequest.deviceName,
                           Device.deviceType == patchRainSensorRequest.deviceType))
                if device is None:
                    patchRainSensorResponse.responseCode = ResponseCodes.NOT_FOUND
                    patchRainSensorResponse.isSuccess = False
                    patchRainSensorResponse.message = f"Device {patchRainSensorRequest.deviceName} not found"
                else:
                    rainSensor: RainSensor | None = await self._session.scalar(select(RainSensor)
                        .where(RainSensor.deviceId == device.deviceId))
                    if rainSensor is None:
                        patchRainSensorResponse.responseCode = ResponseCodes.NOT_FOUND
                        patchRainSensorResponse.isSuccess = False
                        patchRainSensorResponse.message = f"No rain sensor found with the name {device.deviceName}"
                    else:
                        if not GeneralUtils.IsNullOrEmpty(patchRainSensorRequest.model):
                            device.model = patchRainSensorRequest.model

                        if not GeneralUtils.IsNullOrEmpty(patchRainSensorRequest.macAddress):
                            device.macAddress = patchRainSensorRequest.macAddress

                        measureAt: datetime = GeneralUtils.UtcNow()

                        wasRaining: bool = rainSensor.isRaining

                        rainSensor.adcValue = patchRainSensorRequest.adcValue
                        rainSensor.wetnessPercent = patchRainSensorRequest.wetnessPercent
                        rainSensor.isRaining = patchRainSensorRequest.isRaining
                        rainSensor.measureAt = measureAt

                        if patchRainSensorRequest.isRaining:
                            rainSensor.lastDetectedRain = measureAt

                        await self._session.commit()

                        patchRainSensorResponse.rainStarted = not wasRaining and patchRainSensorRequest.isRaining
                        patchRainSensorResponse.responseCode = ResponseCodes.OK
                        patchRainSensorResponse.isSuccess = True
                        patchRainSensorResponse.message = f"Rain Sensor with the name {device.deviceName} updated successfully."
        except Exception as ex:
            await self._session.rollback()
            patchRainSensorResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            patchRainSensorResponse.isSuccess = False
            patchRainSensorResponse.message = f"Unexpected error on RainSensorRepository -> PatchRainSensor: {ex}"

        return patchRainSensorResponse
    #endregion

    # region GetRainingSensor
    async def GetRainingSensor(self) -> GetRainingSensorResponse:
        getRainingSensorResponse: GetRainingSensorResponse = GetRainingSensorResponse()
        try:
            rainingSensors: list[RainSensor] = list(await self._session.scalars(select(RainSensor).where(RainSensor.isRaining)))

            getRainingSensorResponse.rainingSensors = rainingSensors if any(rainingSensors) else []
            getRainingSensorResponse.responseCode = ResponseCodes.OK
            getRainingSensorResponse.isSuccess = True
            getRainingSensorResponse.message = f"raining sensors count: {len(getRainingSensorResponse.rainingSensors)}"
        except Exception as ex:
            getRainingSensorResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            getRainingSensorResponse.isSuccess = False
            getRainingSensorResponse.message = f"Unexpected error on RainSensorRepository -> PatchRainSensor: {ex}"

        return getRainingSensorResponse
    # endregion