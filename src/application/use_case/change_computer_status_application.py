from application.data_transfer_object.change_computer_status.get_computer_status.get_computer_status_response import GetComputerStatusResponse
from application.data_transfer_object.change_computer_status.turn_off_computer.turn_off_computer_response import TurnOffComputerResponse
from application.data_transfer_object.change_computer_status.turn_on_computer.turn_on_computer_response import TurnOnComputerResponse
from application.interface.application.i_change_computer_status_application import IChangeComputerStatusApplication
from application.interface.repository.i_change_computer_status_repository import IChangeComputerStatusRepository

class ChangeComputerStatusApplication(IChangeComputerStatusApplication):

    def __init__(self, change_computer_status_repository: IChangeComputerStatusRepository):
        self._change_computer_status_repository = change_computer_status_repository

    async def get_computer_status(self) -> GetComputerStatusResponse:
        return await self._change_computer_status_repository.get_computer_status()

    async def turn_off_computer(self) -> TurnOffComputerResponse:
        return await self._change_computer_status_repository.turn_off_computer()

    async def turn_on_computer(self) -> TurnOnComputerResponse:
        return await self._change_computer_status_repository.turn_on_computer()
