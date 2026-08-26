from abc import ABC, abstractmethod

class ILightService(ABC):
    @abstractmethod
    async def execute_light_order(self, intent_name: str) -> str: ...