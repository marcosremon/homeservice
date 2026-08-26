from abc import ABC, abstractmethod

from application.data_transfer_object.change_computer_status.get_computer_status.GetComputerStatusResponse import GetComputerStatusResponse
from application.data_transfer_object.change_computer_status.turn_off_computer.TurnOffComputerResponse import TurnOffComputerResponse
from application.data_transfer_object.change_computer_status.turn_on_computer.TurnOnComputerResponse import TurnOnComputerResponse

class IChangeComputerStatusRepository(ABC):
    @abstractmethod
    async def GetComputerStatus(self) -> GetComputerStatusResponse: ...

    @abstractmethod
    async def TurnOffComputer(self) -> TurnOffComputerResponse: ...

    @abstractmethod
    async def TurnOnComputer(self) -> TurnOnComputerResponse: ...