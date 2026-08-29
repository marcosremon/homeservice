from fastapi import APIRouter, Depends
from fastapi_utils.cbv import cbv
from starlette import status
from application.data_transfer_object.home_automation.sensor.temperature_sensor.CreateTemperatureSensor.CreateTemperatureSensorRequest import CreateTemperatureSensorRequest
from application.data_transfer_object.home_automation.sensor.temperature_sensor.CreateTemperatureSensor.CreateTemperatureSensorResponse import CreateTemperatureSensorResponse
from application.data_transfer_object.home_automation.sensor.temperature_sensor.PatchTemperatureSensor.PatchTemperatureSensorRequest import PatchTemperatureSensorRequest
from application.data_transfer_object.home_automation.sensor.temperature_sensor.PatchTemperatureSensor.PatchTemperatureSensorResponse import PatchTemperatureSensorResponse
from application.interface.application.ITemperatureSensorApplication import ITemperatureSensorApplication
from infraestructure.persistence.dependencies.DependencyInjection import GetTemperatureSensorApplication
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.json.ResponseCodesJson import ResponseCodesJson
from transversal.json_interchange.home_automation.sensor.temperature_sensor.CreateTemperatureSensor.CreateTemperatureSensorRequestJson import CreateTemperatureSensorRequestJson
from transversal.json_interchange.home_automation.sensor.temperature_sensor.CreateTemperatureSensor.CreateTemperatureSensorResponseJson import CreateTemperatureSensorResponseJson
from transversal.json_interchange.home_automation.sensor.temperature_sensor.PatchTemperatureSensor.PatchTemperatureSensorRequestJson import PatchTemperatureSensorRequestJson
from transversal.json_interchange.home_automation.sensor.temperature_sensor.PatchTemperatureSensor.PatchTemperatureSensorResponseJson import PatchTemperatureSensorResponseJson
from transversal.security.filter.ApiKeyAuth import ApiKeyAuth

router: APIRouter = APIRouter(
    prefix = "/sensors/temperature-sensor",
    dependencies = [Depends(ApiKeyAuth.GetApiKey)], # filtro de seguridad
)

@cbv(router)
class TemperatureController:
    _temperatureSensorApplication: ITemperatureSensorApplication = Depends(GetTemperatureSensorApplication)

    #region CreateTemperatureSensor
    @router.post("/create-temperature-sensor", response_model = CreateTemperatureSensorResponseJson, status_code = status.HTTP_200_OK)
    async def CreateTemperatureSensor(self, createTemperatureSensorRequestJson: CreateTemperatureSensorRequestJson) -> CreateTemperatureSensorResponseJson:
        createTemperatureSensorResponseJson: CreateTemperatureSensorResponseJson = CreateTemperatureSensorResponseJson()
        try:
            if (GeneralUtils.IsNullOrEmpty(createTemperatureSensorRequestJson.callOut) or
                GeneralUtils.IsNullOrEmpty(createTemperatureSensorRequestJson.deviceName) or
                GeneralUtils.IsNullOrEmpty(createTemperatureSensorRequestJson.deviceType) or
                GeneralUtils.IsNullOrEmpty(createTemperatureSensorRequestJson.model) or
                GeneralUtils.IsNullOrEmpty(createTemperatureSensorRequestJson.manufacturer) or
                createTemperatureSensorRequestJson.temperature == None or
                createTemperatureSensorRequestJson.adcVoltage == None or
                createTemperatureSensorRequestJson.measureAt == None
            ):
                createTemperatureSensorResponseJson.responseCodeJson = ResponseCodesJson.INVALID_DATA
                createTemperatureSensorResponseJson.isSuccess = False
                createTemperatureSensorResponseJson.message = "the data is invalid"
            else:
                createTemperatureSensorRequest: CreateTemperatureSensorRequest = CreateTemperatureSensorRequest(
                    callOut = createTemperatureSensorRequestJson.callOut,
                    deviceName = createTemperatureSensorRequestJson.deviceName,
                    deviceType = createTemperatureSensorRequestJson.deviceType,
                    model = createTemperatureSensorRequestJson.model,
                    manufacturer = createTemperatureSensorRequestJson.manufacturer,
                    macAddress = createTemperatureSensorRequestJson.macAddress,
                    temperature = createTemperatureSensorRequestJson.temperature,
                    adcVoltage = createTemperatureSensorRequestJson.adcVoltage,
                    measureAt = createTemperatureSensorRequestJson.measureAt,
                )

                createTemperatureSensorResponse: CreateTemperatureSensorResponse = await self._temperatureSensorApplication.CreateTemperatureSensor(createTemperatureSensorRequest)

                createTemperatureSensorResponseJson.responseCodeJson = ResponseCodesJson(createTemperatureSensorResponse.responseCode)
                createTemperatureSensorResponseJson.isSuccess = createTemperatureSensorResponse.isSuccess
                createTemperatureSensorResponseJson.message = createTemperatureSensorResponse.message
        except Exception as ex:
            createTemperatureSensorResponseJson.responseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR
            createTemperatureSensorResponseJson.isSuccess = False
            createTemperatureSensorResponseJson.message = f"Ha ocurrido un error al crear el sensor de temperatura {ex}."

        return createTemperatureSensorResponseJson
    # endregion

    #region PatchTemperatureSensor
    @router.post("/patch-temperature-sensor", response_model = PatchTemperatureSensorResponseJson, status_code = status.HTTP_200_OK)
    async def PatchTemperatureSensor(self, patchTemperatureSensorRequestJson: PatchTemperatureSensorRequestJson) -> PatchTemperatureSensorResponseJson:
        patchTemperatureSensorResponseJson: PatchTemperatureSensorResponseJson = PatchTemperatureSensorResponseJson()
        try:
            if (GeneralUtils.IsNullOrEmpty(patchTemperatureSensorRequestJson.callOut) or
                GeneralUtils.IsNullOrEmpty(patchTemperatureSensorRequestJson.deviceName) or
                GeneralUtils.IsNullOrEmpty(patchTemperatureSensorRequestJson.deviceType)
            ):
                patchTemperatureSensorResponseJson.responseCodeJson = ResponseCodesJson.INVALID_DATA
                patchTemperatureSensorResponseJson.isSuccess = False
                patchTemperatureSensorResponseJson.message = "the callOut, device name or device type is invalid"
            else:
                patchTemperatureSensorRequest: PatchTemperatureSensorRequest = PatchTemperatureSensorRequest(
                    callOut = patchTemperatureSensorRequestJson.callOut,
                    deviceName = patchTemperatureSensorRequestJson.deviceName,
                    deviceType = patchTemperatureSensorRequestJson.deviceType,
                    model = patchTemperatureSensorRequestJson.model,
                    macAddress = patchTemperatureSensorRequestJson.macAddress,
                    temperature = patchTemperatureSensorRequestJson.temperature,
                    adcVoltage = patchTemperatureSensorRequestJson.adcVoltage,
                    measureAt = patchTemperatureSensorRequestJson.measureAt,
                )

                patchTemperatureSensorResponse: PatchTemperatureSensorResponse = await self._temperatureSensorApplication.PatchTemperatureSensor(patchTemperatureSensorRequest)

                patchTemperatureSensorResponseJson.responseCodeJson = ResponseCodesJson(patchTemperatureSensorResponse.responseCode)
                patchTemperatureSensorResponseJson.isSuccess = patchTemperatureSensorResponse.isSuccess
                patchTemperatureSensorResponseJson.message = patchTemperatureSensorResponse.message
        except Exception as ex:
            patchTemperatureSensorResponseJson.responseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR
            patchTemperatureSensorResponseJson.isSuccess = False
            patchTemperatureSensorResponseJson.message = f"Ha ocurrido un error al modificar los datos del sensor de temperatura {ex}."

        return patchTemperatureSensorResponseJson
    #endregion