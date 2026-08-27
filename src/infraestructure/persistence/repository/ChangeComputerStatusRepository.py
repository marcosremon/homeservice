from application.data_transfer_object.change_computer_status.get_computer_status.GetComputerStatusResponse import GetComputerStatusResponse
from application.data_transfer_object.change_computer_status.turn_off_computer.TurnOffComputerResponse import TurnOffComputerResponse
from application.data_transfer_object.change_computer_status.turn_on_computer.TurnOnComputerResponse import TurnOnComputerResponse
from application.interface.repository.IChangeComputerStatusRepository import IChangeComputerStatusRepository
from transversal.common.configuration.Settings import Settings
from transversal.common.utils.ComputerStatusUtils import ComputerStatusUtils
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.base.ResponseCodes import ResponseCodes

class ChangeComputerStatusRepository(IChangeComputerStatusRepository):

    def __init__(self, settings: Settings) -> None:
        self._settings = settings

    #region turn_on_computer
    async def TurnOnComputer(self) -> TurnOnComputerResponse:
        turnOnComputerResponse: TurnOnComputerResponse = TurnOnComputerResponse()
        try:
            macAddress: str = self._settings.lanComputerMac
            broadcastIp: str = self._settings.lanBroadcastIp
            ipAddress: str = self._settings.lanComputerIp

            if (GeneralUtils.IsNullOrEmpty(macAddress) or
                GeneralUtils.IsNullOrEmpty(broadcastIp) or
                GeneralUtils.IsNullOrEmpty(ipAddress)
            ):
                turnOnComputerResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
                turnOnComputerResponse.isSuccess = False
                turnOnComputerResponse.message = "Missing LanParameters in settings"
            elif await ComputerStatusUtils.ComputerStatus(ipAddress):
                turnOnComputerResponse.responseCode = ResponseCodes.OK
                turnOnComputerResponse.isSuccess = True
                turnOnComputerResponse.message = "PC already on. Nothing to do."
            else:
                await ComputerStatusUtils.SendWakeOnLanPacket(macAddress, broadcastIp)

                turnOnComputerResponse.responseCode = ResponseCodes.OK
                turnOnComputerResponse.isSuccess = True
                turnOnComputerResponse.message = "WoL sent. PC booting into CachyOS."
        except Exception as ex:
            turnOnComputerResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            turnOnComputerResponse.isSuccess = False
            turnOnComputerResponse.message = f"unexpected error on ChangeComputerStatusRepository -> turn_on_computer: {ex}"

        return turnOnComputerResponse
    #endregion

    #region turn_off_computer
    async def TurnOffComputer(self) -> TurnOffComputerResponse:
        turnOffComputerResponse: TurnOffComputerResponse = TurnOffComputerResponse()
        try:
            ipAddress: str = self._settings.lanComputerIp
            cachyOsUser: str = self._settings.lanCachyosUser

            if GeneralUtils.IsNullOrEmpty(ipAddress) or GeneralUtils.IsNullOrEmpty(cachyOsUser):
                turnOffComputerResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
                turnOffComputerResponse.isSuccess = False
                turnOffComputerResponse.message = "LanParameters incompletos en settings (lan_computer_ip, lan_cachyos_user)"
            else:
                message: str = await ComputerStatusUtils.ShutdownComputer(ipAddress, cachyOsUser)

                turnOffComputerResponse.responseCode = ResponseCodes.OK
                turnOffComputerResponse.isSuccess = True
                turnOffComputerResponse.message = message
        except Exception as ex:
            turnOffComputerResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            turnOffComputerResponse.isSuccess = False
            turnOffComputerResponse.message = f"unexpected error on ChangeComputerStatusRepository -> turn_off_computer: {ex}"

        return turnOffComputerResponse
    #endregion

    #region get_computer_status
    async def GetComputerStatus(self) -> GetComputerStatusResponse:
        getComputerStatusResponse: GetComputerStatusResponse = GetComputerStatusResponse()
        try:
            ipAddress: str = self._settings.lanComputerIp

            if GeneralUtils.IsNullOrEmpty(ipAddress):
                getComputerStatusResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
                getComputerStatusResponse.isSuccess = False
                getComputerStatusResponse.message = "The ip address is empty on settings"
            else:
                ComputerStatus: bool = await ComputerStatusUtils.ComputerStatus(ipAddress)

                getComputerStatusResponse.responseCode = ResponseCodes.OK
                getComputerStatusResponse.ComputerStatus = ComputerStatus
                getComputerStatusResponse.isSuccess = True
                getComputerStatusResponse.message = "ON" if ComputerStatus else "OFF"
        except Exception as ex:
            getComputerStatusResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            getComputerStatusResponse.ComputerStatus = False
            getComputerStatusResponse.isSuccess = False
            getComputerStatusResponse.message = f"unexpected error on ChangeComputerStatusRepository -> get_computer_status: {ex}"

        return getComputerStatusResponse
    #endregion