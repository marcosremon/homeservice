from abc import ABC, abstractmethod

from application.data_transfer_object.alexa.alexa_request import AlexaRequest

class IRoombaService(ABC):
    @abstractmethod
    async def execute_roomba_order(self, intent_name: str, alexa_request: AlexaRequest) -> str: ...