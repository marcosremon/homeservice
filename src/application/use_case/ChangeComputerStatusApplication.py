from application.data_transfer_object.change_computer_status.get_computer_status.GetComputerStatusResponse import GetComputerStatusResponse
from application.data_transfer_object.change_computer_status.turn_off_computer.TurnOffComputerResponse import TurnOffComputerResponse
from application.data_transfer_object.change_computer_status.turn_on_computer.TurnOnComputerResponse import TurnOnComputerResponse
from application.interface.application.IChangeComputerStatusApplication import IChangeComputerStatusApplication
from application.interface.repository.IChangeComputerStatusRepository import IChangeComputerStatusRepository

class ChangeComputerStatusApplication(IChangeComputerStatusApplication):

    def __init__(self, changeComputerStatusRepository: IChangeComputerStatusRepository):
        self._changeComputerStatusRepository: IChangeComputerStatusRepository = changeComputerStatusRepository

    async def GetComputerStatus(self) -> GetComputerStatusResponse:
        return await self._changeComputerStatusRepository.GetComputerStatus()

    async def TurnOffComputer(self) -> TurnOffComputerResponse:
        return await self._changeComputerStatusRepository.TurnOffComputer()

    async def TurnOnComputer(self) -> TurnOnComputerResponse:
        return await self._changeComputerStatusRepository.TurnOnComputer()