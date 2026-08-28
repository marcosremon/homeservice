from abc import ABC, abstractmethod

class IMqttService(ABC):
    @abstractmethod
    async def Publish(self, topic: str, payload: str, retain: bool = True) -> None: ...
