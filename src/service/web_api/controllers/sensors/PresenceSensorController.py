from fastapi_utils.cbv import cbv
from fastapi import APIRouter, Depends, status
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorRequest import CreatePresenceSensorRequest
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorResponse import CreatePresenceSensorResponse
from application.data_transfer_object.home_automation.sensor.presence_sensor.patch_presence_sensor_data.PatchPresenceSensorDataRequest import PatchPresenceSensorDataRequest
from application.data_transfer_object.home_automation.sensor.presence_sensor.patch_presence_sensor_data.PatchPresenceSensorDataResponse import PatchPresenceSensorDataResponse
from application.interface.application.IPresenceSensorApplication import IPresenceSensorApplication
from transversal.security.filter.ApiKeyAuth import GetApiKey
from infraestructure.persistence.dependencies.DependencyInjection import GetPresenceSensorApplication
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.json.ResponseCodesJson import ResponseCodesJson
from transversal.json_interchange.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorRequestJson import CreatePresenceSensorRequestJson
from transversal.json_interchange.home_automation.sensor.presence_sensor.create_presence_sensor.CreatePresenceSensorResponseJson import CreatePresenceSensorResponseJson
from transversal.json_interchange.home_automation.sensor.presence_sensor.patch_presence_sensor_data.PatchPresenceSensorDataRequestJson import PatchPresenceSensorDataRequestJson
from transversal.json_interchange.home_automation.sensor.presence_sensor.patch_presence_sensor_data.PatchPresenceSensorDataResponseJson import PatchPresenceSensorDataResponseJson

router = APIRouter(
    prefix = "/presence-sensor",
    dependencies = [Depends(GetApiKey)],  # equivalente a [ApiKeyAuth] a nivel de clase
)

@cbv(router)
class PresenceSensorController:
    _presenceSensorApplication: IPresenceSensorApplication = Depends(GetPresenceSensorApplication)

    #region CreatePresenceSensor
    @router.post("/create-presence-sensor", response_model = CreatePresenceSensorResponseJson, status_code = status.HTTP_200_OK)
    async def CreatePresenceSensor(self, createPresenceSensorRequestJson: CreatePresenceSensorRequestJson) -> CreatePresenceSensorResponseJson:
        createPresenceSensorResponseJson: CreatePresenceSensorResponseJson = CreatePresenceSensorResponseJson()
        try:
            if (GeneralUtils.IsNullOrEmpty(createPresenceSensorRequestJson.callout) or
                GeneralUtils.IsNullOrEmpty(createPresenceSensorRequestJson.deviceName) or
                GeneralUtils.IsNullOrEmpty(createPresenceSensorRequestJson.deviceType)
            ):
                createPresenceSensorResponseJson.responseCodeJson = ResponseCodesJson.INVALID_DATA
                createPresenceSensorResponseJson.isSuccess = False
                createPresenceSensorResponseJson.message = "the callout, device name or device type is invalid"
            else:
                createPresenceSensorRequest: CreatePresenceSensorRequest = CreatePresenceSensorRequest(
                    callout = createPresenceSensorRequestJson.callout,
                    deviceName = createPresenceSensorRequestJson.deviceName,
                    deviceType = createPresenceSensorRequestJson.deviceType,
                    ts = createPresenceSensorRequestJson.ts,
                    presence = createPresenceSensorRequestJson.presence,
                    distanceCm = createPresenceSensorRequestJson.distanceCm,
                    motion = createPresenceSensorRequestJson.motion,
                    lastDetectedPresence = createPresenceSensorRequestJson.lastDetectedPresence,
                    model = createPresenceSensorRequestJson.model,
                    manufacturer = createPresenceSensorRequestJson.manufacturer,
                    macAddress = createPresenceSensorRequestJson.macAddress,
                )

                createPresenceSensorResponse: CreatePresenceSensorResponse = await self._presenceSensorApplication.CreatePresenceSensor(createPresenceSensorRequest)

                createPresenceSensorResponseJson.responseCodeJson = ResponseCodesJson(createPresenceSensorResponse.responseCode)
                createPresenceSensorResponseJson.isSuccess = createPresenceSensorResponse.isSuccess
                createPresenceSensorResponseJson.message = createPresenceSensorResponse.message
        except Exception as ex:
            createPresenceSensorResponseJson.responseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR
            createPresenceSensorResponseJson.isSuccess = False
            createPresenceSensorResponseJson.message = f"Ha ocurrido un error al crear el sensor de presencia {ex}."

        return createPresenceSensorResponseJson
    #endregion

    #region PatchPresenceSensorData
    @router.post("/patch-presence-sensor-data", response_model = PatchPresenceSensorDataResponseJson, status_code = status.HTTP_200_OK)
    async def PatchPresenceSensorData(self, patchPresenceSensorDataRequestJson: PatchPresenceSensorDataRequestJson) -> PatchPresenceSensorDataResponseJson:
        patchPresenceSensorDataResponseJson: PatchPresenceSensorDataResponseJson = PatchPresenceSensorDataResponseJson()
        try:
            if (GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequestJson.callout) or
                GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequestJson.deviceName) or
                GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequestJson.deviceType)
            ):
                patchPresenceSensorDataResponseJson.responseCodeJson = ResponseCodesJson.INVALID_DATA
                patchPresenceSensorDataResponseJson.isSuccess = False
                patchPresenceSensorDataResponseJson.message = "the callout, device name or device type is invalid"
            else:
                patchPresenceSensorDataRequest: PatchPresenceSensorDataRequest = PatchPresenceSensorDataRequest(
                    callout = patchPresenceSensorDataRequestJson.callout,
                    deviceName = patchPresenceSensorDataRequestJson.deviceName,
                    deviceType = patchPresenceSensorDataRequestJson.deviceType,
                    model = patchPresenceSensorDataRequestJson.model,
                    manufacturer = patchPresenceSensorDataRequestJson.manufacturer,
                    macAddress = patchPresenceSensorDataRequestJson.macAddress,
                    ts = patchPresenceSensorDataRequestJson.ts,
                    presence = patchPresenceSensorDataRequestJson.presence,
                    distanceCm = patchPresenceSensorDataRequestJson.distanceCm,
                    motion = patchPresenceSensorDataRequestJson.motion
                )

                patchPresenceSensorDataResponse: PatchPresenceSensorDataResponse = await self._presenceSensorApplication.PatchPresenceSensorData(patchPresenceSensorDataRequest)

                patchPresenceSensorDataResponseJson.responseCodeJson = ResponseCodesJson(patchPresenceSensorDataResponse.responseCode)
                patchPresenceSensorDataResponseJson.isSuccess = patchPresenceSensorDataResponse.isSuccess
                patchPresenceSensorDataResponseJson.message = patchPresenceSensorDataResponse.message
        except Exception as ex:
            patchPresenceSensorDataResponseJson.responseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR
            patchPresenceSensorDataResponseJson.isSuccess = False
            patchPresenceSensorDataResponseJson.message = f"Ha ocurrido un error al modificar los datos del sensor de presencia {ex}."

        return patchPresenceSensorDataResponseJson
    #endregion