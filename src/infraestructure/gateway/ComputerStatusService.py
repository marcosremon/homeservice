from application.interface.service.IComputerStatusService import IComputerStatusService
from transversal.common.configuration.Settings import Settings, GetSettings
from transversal.common.utils.ComputerStatusUtils import ComputerStatusUtils

class ComputerStatusService(IComputerStatusService):

    def __init__(self, settings: Settings):
        self._settings: Settings = settings

    # region send_alexa_order
    async def ExecuteComputerStatusOrder(self, intentName: str) -> str:
        computerAddress: str = self._settings.lanComputerIp

        message: str = ""
        computerStatus: bool = await ComputerStatusUtils.ComputerStatus(computerAddress)

        if intentName == "computer_status_order_encender_ordenador" and computerStatus: message = "El ordenador ya está encendido."
        if intentName == "computer_status_order_apagar_ordenador" and not computerStatus: message = "El ordenador ya está apagado."
        if intentName == "computer_status_order_get_status": message = "El ordenador está encendido." if computerStatus else "El ordenador está apagado.";

        return message
    # endregion