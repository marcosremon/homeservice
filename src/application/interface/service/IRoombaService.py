from abc import ABC, abstractmethod

from application.data_transfer_object.alexa.AlexaRequest import AlexaRequest

class IRoombaService(ABC):
    @abstractmethod
    async def ExecuteRoombaOrder(self, intentName: str, alexaRequest: AlexaRequest) -> str: ...