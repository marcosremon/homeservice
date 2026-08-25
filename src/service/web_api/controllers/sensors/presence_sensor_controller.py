from fastapi_utils.cbv import cbv
from fastapi import APIRouter, Depends, status
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.create_presence_sensor_request import CreatePresenceSensorRequest
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.create_presence_sensor_response import CreatePresenceSensorResponse
from application.interface.application.i_presence_sensor_application import IPresenceSensorApplication
from infraestructure.security.api_key_auth import get_api_key
from infraestructure.persistence.dependencies.dependency_injection import get_presence_sensor_application
from transversal.common.wrappers.json.response_codes_json import ResponseCodesJson
from transversal.json_interchange.home_automation.sensor.presence_sensor.create_presence_sensor.create_presence_sensor_request_json import CreatePresenceSensorRequestJson
from transversal.json_interchange.home_automation.sensor.presence_sensor.create_presence_sensor.create_presence_sensor_response_json import CreatePresenceSensorResponseJson

router = APIRouter(
    prefix = "/presence-sensor",
    dependencies = [Depends(get_api_key)],  # equivalente a [ApiKeyAuth] a nivel de clase
)

@cbv(router)
class PresenceSensorController:
    _presence_sensor_application: IPresenceSensorApplication = Depends(get_presence_sensor_application)

    # region CreatePresenceSensor
    @router.post("/create-presence-sensor", response_model = CreatePresenceSensorResponseJson, status_code = status.HTTP_200_OK)
    async def create_presence_sensor(self, create_presence_sensor_request_json: CreatePresenceSensorRequestJson) -> CreatePresenceSensorResponseJson:
        create_presence_sensor_response_json: CreatePresenceSensorResponseJson = CreatePresenceSensorResponseJson()
        try:
            if (not create_presence_sensor_request_json.call_out
                or not create_presence_sensor_request_json.device_name
                or not create_presence_sensor_request_json.device_type
            ):
                create_presence_sensor_response_json.response_code_json = ResponseCodesJson.INVALID_DATA
                create_presence_sensor_response_json.is_success = False
                create_presence_sensor_response_json.message = "the callout, device name or device type is invalid"
            else:
                create_presence_sensor_request: CreatePresenceSensorRequest = CreatePresenceSensorRequest(
                    call_out=create_presence_sensor_request_json.call_out,
                    device_name=create_presence_sensor_request_json.device_name,
                    device_type=create_presence_sensor_request_json.device_type,
                    ts=create_presence_sensor_request_json.ts,
                    presence=create_presence_sensor_request_json.presence,
                    distance_cm=create_presence_sensor_request_json.distance_cm,
                    motion=create_presence_sensor_request_json.motion,
                    last_detected_presence=create_presence_sensor_request_json.last_detected_presence,
                    model=create_presence_sensor_request_json.model,
                    manufacturer=create_presence_sensor_request_json.manufacturer,
                    mac_address=create_presence_sensor_request_json.mac_address,
                )

                create_presence_sensor_response: CreatePresenceSensorResponse = await self._presence_sensor_application.create_presence_sensor(create_presence_sensor_request)

                create_presence_sensor_response_json.response_code_json = ResponseCodesJson(create_presence_sensor_response.response_code)
                create_presence_sensor_response_json.is_success = create_presence_sensor_response.is_success
                create_presence_sensor_response_json.message = create_presence_sensor_response.message
        except Exception as ex:
            create_presence_sensor_response_json.response_code_json = ResponseCodesJson.INTERNAL_SERVER_ERROR
            create_presence_sensor_response_json.is_success = False
            create_presence_sensor_response_json.message = f"Ha ocurrido un error al crear el sensor de presencia {ex}."

        return create_presence_sensor_response_json
    # endregion