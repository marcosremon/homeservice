from application.data_transfer_object.alexa.AlexaRequest import AlexaRequest
from application.data_transfer_object.alexa.AlexaResponse import AlexaResponse
from application.interface.application.IAlexaApplication import IAlexaApplication
from transversal.common.alexa.alexa_response.AlexaOutputSpeech import AlexaOutputSpeech
from transversal.common.alexa.alexa_response.AlexaResponseContent import AlexaResponseContent

class AlexaApplication(IAlexaApplication):

    async def SendAlexaOrder(self, alexaRequest: AlexaRequest) -> AlexaResponse:
        # TODO (Gateway): en C# esta clase solo delega en IAlexaService, que es quien
        # enruta por IntentName (roomba / luces / ordenador) y habla con Gemini para el
        # ConversationIntent. Falta portar ese servicio e inyectarlo aqui. De momento se
        # devuelve una respuesta valida para Alexa para no romper el contrato del skill.
        return AlexaResponse(
            version = alexaRequest.version,
            alexaResponseContent = AlexaResponseContent(
                outputSpeech = AlexaOutputSpeech(type = "PlainText", text = "Funcionalidad todavia no disponible."),
                shouldEndSession = True,
            ),
        )