from fastapi_utils.cbv import cbv
from fastapi import APIRouter, Depends, status
from application.data_transfer_object.change_computer_status.get_computer_status.get_computer_status_response import GetComputerStatusResponse
from application.data_transfer_object.change_computer_status.turn_off_computer.turn_off_computer_response import TurnOffComputerResponse
from application.data_transfer_object.change_computer_status.turn_on_computer.turn_on_computer_response import TurnOnComputerResponse
from application.interface.application.i_change_computer_status_application import IChangeComputerStatusApplication
from transversal.security.filter.api_key_auth import get_api_key
from infraestructure.persistence.dependencies.dependency_injection import get_change_computer_status_application
from transversal.common.wrappers.json.response_codes_json import ResponseCodesJson
from transversal.json_interchange.change_computer_status.get_computer_status.get_computer_status_response_json import GetComputerStatusResponseJson
from transversal.json_interchange.change_computer_status.turn_off_computer.turn_off_computer_response_json import TurnOffComputerResponseJson
from transversal.json_interchange.change_computer_status.turn_on_computer.turn_on_computer_response_json import TurnOnComputerResponseJson

router = APIRouter(
    prefix = "/change-computer-status",
    dependencies = [Depends(get_api_key)],  # equivalente a [ApiKeyAuth] a nivel de clase
)

@cbv(router)
class ChangeComputerStatusController:
    _change_computer_status_application: IChangeComputerStatusApplication = Depends(get_change_computer_status_application)

    #region TurnOnComputer
    @router.get("/turn-on-computer", response_model = TurnOnComputerResponseJson, status_code = status.HTTP_200_OK)
    async def turn_on_computer(self) -> TurnOnComputerResponseJson:
        turn_on_computer_response_json: TurnOnComputerResponseJson = TurnOnComputerResponseJson()
        try:
            turn_on_computer_response: TurnOnComputerResponse = await self._change_computer_status_application.turn_on_computer()

            turn_on_computer_response_json.response_code_json = ResponseCodesJson(turn_on_computer_response.response_code)
            turn_on_computer_response_json.is_success = turn_on_computer_response.is_success
            turn_on_computer_response_json.message = turn_on_computer_response.message
        except Exception as ex:
            turn_on_computer_response_json.response_code_json = ResponseCodesJson.INTERNAL_SERVER_ERROR
            turn_on_computer_response_json.is_success = False
            turn_on_computer_response_json.message = f"Ha ocurrido un error al encender el ordenador {ex}."

        return turn_on_computer_response_json
    #endregion

    #region TurnOffComputer
    @router.get("/turn-off-computer", response_model = TurnOffComputerResponseJson, status_code = status.HTTP_200_OK)
    async def turn_off_computer(self) -> TurnOffComputerResponseJson:
        turn_off_computer_response_json: TurnOffComputerResponseJson = TurnOffComputerResponseJson()
        try:
            turn_off_computer_response: TurnOffComputerResponse = await self._change_computer_status_application.turn_off_computer()

            turn_off_computer_response_json.response_code_json = ResponseCodesJson(turn_off_computer_response.response_code)
            turn_off_computer_response_json.is_success = turn_off_computer_response.is_success
            turn_off_computer_response_json.message = turn_off_computer_response.message
        except Exception as ex:
            turn_off_computer_response_json.response_code_json = ResponseCodesJson.INTERNAL_SERVER_ERROR
            turn_off_computer_response_json.is_success = False
            turn_off_computer_response_json.message = f"Ha ocurrido un error al apagar el ordenador {ex}."

        return turn_off_computer_response_json
    #endregion

    #region GetComputerStatus
    @router.get("/get-computer-status", response_model = GetComputerStatusResponseJson, status_code = status.HTTP_200_OK)
    async def get_computer_status(self) -> GetComputerStatusResponseJson:
        get_computer_status_response_json: GetComputerStatusResponseJson = GetComputerStatusResponseJson()
        try:
            get_computer_status_response: GetComputerStatusResponse = await self._change_computer_status_application.get_computer_status()

            get_computer_status_response_json.response_code_json = ResponseCodesJson(get_computer_status_response.response_code)
            get_computer_status_response_json.is_success = get_computer_status_response.is_success
            get_computer_status_response_json.computer_status = get_computer_status_response.computer_status
            get_computer_status_response_json.message = get_computer_status_response.message
        except Exception as ex:
            get_computer_status_response_json.response_code_json = ResponseCodesJson.INTERNAL_SERVER_ERROR
            get_computer_status_response_json.is_success = False
            get_computer_status_response_json.computer_status = False
            get_computer_status_response_json.message = f"Ha ocurrido un error al consultar el estado del ordenador {ex}."

        return get_computer_status_response_json
    #endregion
