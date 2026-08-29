from fastapi_utils.cbv import cbv
from fastapi import APIRouter, Depends, status
from application.data_transfer_object.change_computer_status.get_computer_status.GetComputerStatusResponse import GetComputerStatusResponse
from application.data_transfer_object.change_computer_status.turn_off_computer.TurnOffComputerResponse import TurnOffComputerResponse
from application.data_transfer_object.change_computer_status.turn_on_computer.TurnOnComputerResponse import TurnOnComputerResponse
from application.interface.application.IChangeComputerStatusApplication import IChangeComputerStatusApplication
from transversal.security.filter.ApiKeyAuth import ApiKeyAuth
from infraestructure.persistence.dependencies.DependencyInjection import GetChangeComputerStatusApplication
from transversal.common.wrappers.json.ResponseCodesJson import ResponseCodesJson
from transversal.json_interchange.change_computer_status.get_computer_status.GetComputerStatusResponseJson import GetComputerStatusResponseJson
from transversal.json_interchange.change_computer_status.turn_off_computer.TurnOffComputerResponseJson import TurnOffComputerResponseJson
from transversal.json_interchange.change_computer_status.turn_on_computer.TurnOnComputerResponseJson import TurnOnComputerResponseJson

router: APIRouter = APIRouter(
    prefix = "/change-computer-status",
    dependencies = [Depends(ApiKeyAuth.GetApiKey)], # filtro de seguridad
)

@cbv(router)
class ChangeComputerStatusController:
    _changeComputerStatusApplication: IChangeComputerStatusApplication = Depends(GetChangeComputerStatusApplication)

    #region TurnOnComputer
    @router.get("/turn-on-computer", response_model = TurnOnComputerResponseJson, status_code = status.HTTP_200_OK)
    async def TurnOnComputer(self) -> TurnOnComputerResponseJson:
        turnOnComputerResponseJson: TurnOnComputerResponseJson = TurnOnComputerResponseJson()
        try:
            turnOnComputerResponse: TurnOnComputerResponse = await self._changeComputerStatusApplication.TurnOnComputer()

            turnOnComputerResponseJson.responseCodeJson = ResponseCodesJson(turnOnComputerResponse.responseCode)
            turnOnComputerResponseJson.isSuccess = turnOnComputerResponse.isSuccess
            turnOnComputerResponseJson.message = turnOnComputerResponse.message
        except Exception as ex:
            turnOnComputerResponseJson.responseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR
            turnOnComputerResponseJson.isSuccess = False
            turnOnComputerResponseJson.message = f"Ha ocurrido un error al encender el ordenador {ex}."

        return turnOnComputerResponseJson
    #endregion

    #region TurnOffComputer
    @router.get("/turn-off-computer", response_model = TurnOffComputerResponseJson, status_code = status.HTTP_200_OK)
    async def TurnOffComputer(self) -> TurnOffComputerResponseJson:
        turnOffComputerResponseJson: TurnOffComputerResponseJson = TurnOffComputerResponseJson()
        try:
            turnOffComputerResponse: TurnOffComputerResponse = await self._changeComputerStatusApplication.TurnOffComputer()

            turnOffComputerResponseJson.responseCodeJson = ResponseCodesJson(turnOffComputerResponse.responseCode)
            turnOffComputerResponseJson.isSuccess = turnOffComputerResponse.isSuccess
            turnOffComputerResponseJson.message = turnOffComputerResponse.message
        except Exception as ex:
            turnOffComputerResponseJson.responseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR
            turnOffComputerResponseJson.isSuccess = False
            turnOffComputerResponseJson.message = f"Ha ocurrido un error al apagar el ordenador {ex}."

        return turnOffComputerResponseJson
    #endregion

    #region GetComputerStatus
    @router.get("/get-computer-status", response_model = GetComputerStatusResponseJson, status_code = status.HTTP_200_OK)
    async def GetComputerStatus(self) -> GetComputerStatusResponseJson:
        getComputerStatusResponseJson: GetComputerStatusResponseJson = GetComputerStatusResponseJson()
        try:
            getComputerStatusResponse: GetComputerStatusResponse = await self._changeComputerStatusApplication.GetComputerStatus()

            getComputerStatusResponseJson.responseCodeJson = ResponseCodesJson(getComputerStatusResponse.responseCode)
            getComputerStatusResponseJson.isSuccess = getComputerStatusResponse.isSuccess
            getComputerStatusResponseJson.ComputerStatus = getComputerStatusResponse.ComputerStatus
            getComputerStatusResponseJson.message = getComputerStatusResponse.message
        except Exception as ex:
            getComputerStatusResponseJson.responseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR
            getComputerStatusResponseJson.isSuccess = False
            getComputerStatusResponseJson.ComputerStatus = False
            getComputerStatusResponseJson.message = f"Ha ocurrido un error al consultar el estado del ordenador {ex}."

        return getComputerStatusResponseJson
    #endregion