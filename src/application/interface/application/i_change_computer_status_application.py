from abc import ABC, abstractmethod

from application.data_transfer_object.change_computer_status.get_computer_status.get_computer_status_response import GetComputerStatusResponse
from application.data_transfer_object.change_computer_status.turn_off_computer.turn_off_computer_response import TurnOffComputerResponse
from application.data_transfer_object.change_computer_status.turn_on_computer.turn_on_computer_response import TurnOnComputerResponse

class IChangeComputerStatusApplication(ABC):
    @abstractmethod
    async def get_computer_status(self) -> GetComputerStatusResponse: ...

    @abstractmethod
    async def turn_off_computer(self) -> TurnOffComputerResponse: ...

    @abstractmethod
    async def turn_on_computer(self) -> TurnOnComputerResponse: ...