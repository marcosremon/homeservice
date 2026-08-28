from abc import ABC, abstractmethod

from application.data_transfer_object.alexa.AlexaRequest import AlexaRequest

class ITemperatureSensorService(ABC):

    @abstractmethod
    async def ExecuteTemperatureSensorOrder(self, intentName: str, alexaRequest: AlexaRequest) -> str: ...
