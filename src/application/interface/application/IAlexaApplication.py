from abc import ABC, abstractmethod

from application.data_transfer_object.alexa.AlexaRequest import AlexaRequest
from application.data_transfer_object.alexa.AlexaResponse import AlexaResponse

class IAlexaApplication(ABC):
    @abstractmethod
    async def SendAlexaOrder(self, alexaRequest: AlexaRequest) -> AlexaResponse: ...