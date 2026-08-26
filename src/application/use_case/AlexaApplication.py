from application.data_transfer_object.alexa.AlexaRequest import AlexaRequest
from application.data_transfer_object.alexa.AlexaResponse import AlexaResponse
from application.interface.application.IAlexaApplication import IAlexaApplication
from application.interface.service.IAlexaService import IAlexaService

class AlexaApplication(IAlexaApplication):

    def __init__(self, alexaService: IAlexaService):
        self._alexaService = alexaService

    async def SendAlexaOrder(self, alexaRequest: AlexaRequest) -> AlexaResponse:
        return await self._alexaService.SendAlexaOrder(alexaRequest)