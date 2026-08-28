from abc import ABC, abstractmethod

class IComputerStatusService(ABC):

    @abstractmethod
    async def ExecuteComputerStatusOrder(self, intentName: str) -> str: ...