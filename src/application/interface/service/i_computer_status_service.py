from abc import ABC, abstractmethod

class IComputerStatusService(ABC):
    @abstractmethod
    async def execute_computer_status_order(self, intent_name: str) -> str: ...