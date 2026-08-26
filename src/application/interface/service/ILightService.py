from abc import ABC, abstractmethod

class ILightService(ABC):
    @abstractmethod
    async def ExecuteLightOrder(self, intentName: str) -> str: ...