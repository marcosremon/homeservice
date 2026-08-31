from fastapi import APIRouter, Depends
from fastapi_utils.cbv import cbv
from starlette import status
from application.data_transfer_object.home_automation.sensor.rain_sensor.CreateRainSensor.CreateRainSensorRequest import CreateRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.CreateRainSensor.CreateRainSensorResponse import CreateRainSensorResponse
from application.data_transfer_object.home_automation.sensor.rain_sensor.PatchRainSensor.PatchRainSensorRequest import PatchRainSensorRequest
from application.data_transfer_object.home_automation.sensor.rain_sensor.PatchRainSensor.PatchRainSensorResponse import PatchRainSensorResponse
from application.interface.application.IRainSensorApplication import IRainSensorApplication
from infraestructure.persistence.dependencies.DependencyInjection import GetRainSensorApplication
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.json.ResponseCodesJson import ResponseCodesJson
from transversal.json_interchange.home_automation.sensor.rain_sensor.CreateRainSensor.CreateRainSensorRequestJson import CreateRainSensorRequestJson
from transversal.json_interchange.home_automation.sensor.rain_sensor.CreateRainSensor.CreateRainSensorResponseJson import CreateRainSensorResponseJson
from transversal.json_interchange.home_automation.sensor.rain_sensor.PatchRainSensor.PatchRainSensorRequestJson import PatchRainSensorRequestJson
from transversal.json_interchange.home_automation.sensor.rain_sensor.PatchRainSensor.PatchRainSensorResponseJson import PatchRainSensorResponseJson
from transversal.security.filter.ApiKeyAuth import ApiKeyAuth

router: APIRouter = APIRouter(
    prefix = "/sensors/rain-sensor",
    dependencies = [Depends(ApiKeyAuth.GetApiKey)], # filtro de seguridad
)

@cbv(router)
class RainSensorController:
    _rainSensorApplication: IRainSensorApplication = Depends(GetRainSensorApplication)

    #region CreateRainSensor
    @router.post("/create-rain-sensor", response_model = CreateRainSensorResponseJson, status_code = status.HTTP_200_OK)
    async def CreateRainSensor(self, createRainSensorRequestJson: CreateRainSensorRequestJson) -> CreateRainSensorResponseJson:
        createRainSensorResponseJson: CreateRainSensorResponseJson = CreateRainSensorResponseJson()
        try:
            if (GeneralUtils.IsNullOrEmpty(createRainSensorRequestJson.callOut) or
                GeneralUtils.IsNullOrEmpty(createRainSensorRequestJson.deviceName) or
                GeneralUtils.IsNullOrEmpty(createRainSensorRequestJson.deviceType) or
                GeneralUtils.IsNullOrEmpty(createRainSensorRequestJson.model) or
                GeneralUtils.IsNullOrEmpty(createRainSensorRequestJson.manufacturer)
            ):
                createRainSensorResponseJson.responseCodeJson = ResponseCodesJson.INVALID_DATA
                createRainSensorResponseJson.isSuccess = False
                createRainSensorResponseJson.message = "the data is invalid"
            else:
                createRainSensorRequest: CreateRainSensorRequest = CreateRainSensorRequest(
                    callOut = createRainSensorRequestJson.callOut,
                    deviceName = createRainSensorRequestJson.deviceName,
                    deviceType = createRainSensorRequestJson.deviceType,
                    model = createRainSensorRequestJson.model,
                    manufacturer = createRainSensorRequestJson.manufacturer,
                    macAddress = createRainSensorRequestJson.macAddress,
                    adcValue = createRainSensorRequestJson.adcValue,
                    wetnessPercent = createRainSensorRequestJson.wetnessPercent,
                    isRaining = createRainSensorRequestJson.isRaining,
                    measureAt = GeneralUtils.ToNaiveUtc(createRainSensorRequestJson.measureAt),
                )

                createRainSensorResponse: CreateRainSensorResponse = await self._rainSensorApplication.CreateRainSensor(createRainSensorRequest)

                createRainSensorResponseJson.responseCodeJson = ResponseCodesJson(createRainSensorResponse.responseCode)
                createRainSensorResponseJson.isSuccess = createRainSensorResponse.isSuccess
                createRainSensorResponseJson.message = createRainSensorResponse.message
        except Exception as ex:
            createRainSensorResponseJson.responseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR
            createRainSensorResponseJson.isSuccess = False
            createRainSensorResponseJson.message = f"Ha ocurrido un error al crear el sensor de lluvia {ex}."

        return createRainSensorResponseJson
    # endregion

    #region PatchRainSensor
    @router.post("/patch-rain-sensor", response_model = PatchRainSensorResponseJson, status_code = status.HTTP_200_OK)
    async def PatchRainSensor(self, patchRainSensorRequestJson: PatchRainSensorRequestJson) -> PatchRainSensorResponseJson:
        patchRainSensorResponseJson: PatchRainSensorResponseJson = PatchRainSensorResponseJson()
        try:
            if (GeneralUtils.IsNullOrEmpty(patchRainSensorRequestJson.callOut) or
                GeneralUtils.IsNullOrEmpty(patchRainSensorRequestJson.deviceName) or
                GeneralUtils.IsNullOrEmpty(patchRainSensorRequestJson.deviceType)
            ):
                patchRainSensorResponseJson.responseCodeJson = ResponseCodesJson.INVALID_DATA
                patchRainSensorResponseJson.isSuccess = False
                patchRainSensorResponseJson.message = "the callOut, device name or device type is invalid"
            else:
                patchRainSensorRequest: PatchRainSensorRequest = PatchRainSensorRequest(
                    callOut = patchRainSensorRequestJson.callOut,
                    deviceName = patchRainSensorRequestJson.deviceName,
                    deviceType = patchRainSensorRequestJson.deviceType,
                    model = patchRainSensorRequestJson.model,
                    macAddress = patchRainSensorRequestJson.macAddress,
                    adcValue = patchRainSensorRequestJson.adcValue,
                    wetnessPercent = patchRainSensorRequestJson.wetnessPercent,
                    isRaining = patchRainSensorRequestJson.isRaining,
                    measureAt = GeneralUtils.ToNaiveUtc(patchRainSensorRequestJson.measureAt),
                )

                patchRainSensorResponse: PatchRainSensorResponse = await self._rainSensorApplication.PatchRainSensor(patchRainSensorRequest)

                patchRainSensorResponseJson.responseCodeJson = ResponseCodesJson(patchRainSensorResponse.responseCode)
                patchRainSensorResponseJson.isSuccess = patchRainSensorResponse.isSuccess
                patchRainSensorResponseJson.message = patchRainSensorResponse.message
                patchRainSensorResponseJson.rainStarted = patchRainSensorResponse.rainStarted
        except Exception as ex:
            patchRainSensorResponseJson.responseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR
            patchRainSensorResponseJson.isSuccess = False
            patchRainSensorResponseJson.message = f"Ha ocurrido un error al modificar los datos del sensor de lluvia {ex}."

        return patchRainSensorResponseJson
    #endregion