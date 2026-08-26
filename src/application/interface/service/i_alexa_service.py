from abc import ABC, abstractmethod

from application.data_transfer_object.alexa.alexa_request import AlexaRequest
from application.data_transfer_object.alexa.alexa_response import AlexaResponse

class IAlexaService(ABC):
    @abstractmethod
    async def send_alexa_order(self, alexa_request: AlexaRequest) -> AlexaResponse: ...