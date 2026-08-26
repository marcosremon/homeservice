from application.data_transfer_object.change_computer_status.get_computer_status.get_computer_status_response import GetComputerStatusResponse
from application.data_transfer_object.change_computer_status.turn_off_computer.turn_off_computer_response import TurnOffComputerResponse
from application.data_transfer_object.change_computer_status.turn_on_computer.turn_on_computer_response import TurnOnComputerResponse
from application.interface.repository.i_change_computer_status_repository import IChangeComputerStatusRepository
from transversal.common.configuration.settings import Settings
from transversal.common.utils import computer_status_utils
from transversal.common.utils.general_utils import GeneralUtils
from transversal.common.wrappers.base.response_codes import ResponseCodes

class ChangeComputerStatusRepository(IChangeComputerStatusRepository):

    def __init__(self, settings: Settings) -> None:
        self._settings = settings

    #region turn_on_computer
    async def turn_on_computer(self) -> TurnOnComputerResponse:
        turn_on_computer_response: TurnOnComputerResponse = TurnOnComputerResponse()
        try:
            mac_address: str = self._settings.lan_computer_mac
            broadcast_ip: str = self._settings.lan_broadcast_ip
            ip_address: str = self._settings.lan_computer_ip

            if (GeneralUtils.is_null_or_empty(mac_address) or
                GeneralUtils.is_null_or_empty(broadcast_ip) or
                GeneralUtils.is_null_or_empty(ip_address)
            ):
                turn_on_computer_response.response_code = ResponseCodes.UNEXPECTED_ERROR
                turn_on_computer_response.is_success = False
                turn_on_computer_response.message = "Missing LanParameters in settings"
            elif await computer_status_utils.computer_status(ip_address):
                turn_on_computer_response.response_code = ResponseCodes.OK
                turn_on_computer_response.is_success = True
                turn_on_computer_response.message = "PC already on. Nothing to do."
            else:
                await computer_status_utils.send_wake_on_lan_packet(mac_address, broadcast_ip)

                turn_on_computer_response.response_code = ResponseCodes.OK
                turn_on_computer_response.is_success = True
                turn_on_computer_response.message = "WoL sent. PC booting into CachyOS."
        except Exception as ex:
            turn_on_computer_response.response_code = ResponseCodes.UNEXPECTED_ERROR
            turn_on_computer_response.is_success = False
            turn_on_computer_response.message = f"unexpected error on ChangeComputerStatusRepository -> turn_on_computer: {ex}"

        return turn_on_computer_response
    #endregion

    #region turn_off_computer
    async def turn_off_computer(self) -> TurnOffComputerResponse:
        turn_off_computer_response: TurnOffComputerResponse = TurnOffComputerResponse()
        try:
            ip_address: str = self._settings.lan_computer_ip
            cachy_os_user: str = self._settings.lan_cachyos_user

            if GeneralUtils.is_null_or_empty(ip_address) or GeneralUtils.is_null_or_empty(cachy_os_user):
                turn_off_computer_response.response_code = ResponseCodes.UNEXPECTED_ERROR
                turn_off_computer_response.is_success = False
                turn_off_computer_response.message = "LanParameters incompletos en settings (lan_computer_ip, lan_cachyos_user)"
            else:
                message: str = await computer_status_utils.shutdown_computer(ip_address, cachy_os_user)

                turn_off_computer_response.response_code = ResponseCodes.OK
                turn_off_computer_response.is_success = True
                turn_off_computer_response.message = message
        except Exception as ex:
            turn_off_computer_response.response_code = ResponseCodes.UNEXPECTED_ERROR
            turn_off_computer_response.is_success = False
            turn_off_computer_response.message = f"unexpected error on ChangeComputerStatusRepository -> turn_off_computer: {ex}"

        return turn_off_computer_response
    #endregion

    #region get_computer_status
    async def get_computer_status(self) -> GetComputerStatusResponse:
        get_computer_status_response: GetComputerStatusResponse = GetComputerStatusResponse()
        try:
            ip_address: str = self._settings.lan_computer_ip

            if GeneralUtils.is_null_or_empty(ip_address):
                get_computer_status_response.response_code = ResponseCodes.UNEXPECTED_ERROR
                get_computer_status_response.is_success = False
                get_computer_status_response.message = "The ip address is empty on settings"
            else:
                computer_status: bool = await computer_status_utils.computer_status(ip_address)

                get_computer_status_response.response_code = ResponseCodes.OK
                get_computer_status_response.computer_status = computer_status
                get_computer_status_response.is_success = True
                get_computer_status_response.message = "ON" if computer_status else "OFF"
        except Exception as ex:
            get_computer_status_response.response_code = ResponseCodes.UNEXPECTED_ERROR
            get_computer_status_response.computer_status = False
            get_computer_status_response.is_success = False
            get_computer_status_response.message = f"unexpected error on ChangeComputerStatusRepository -> get_computer_status: {ex}"

        return get_computer_status_response
    #endregion